using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static PKHeX.WinForms.PluginLoadSetting;

namespace PKHeX.WinForms;

public static class PluginLoader
{
    public static IEnumerable<T> LoadPlugins<T>(string pluginPath, PluginLoadSetting loadSetting) where T : class
    {
        var dllFileNames = !Directory.Exists(pluginPath)
            ? [] // Don't immediately return, as we may be loading plugins merged with this .exe
            : Directory.EnumerateFiles(pluginPath, "*.dll", SearchOption.AllDirectories);
        var assemblies = GetAssemblies(dllFileNames, loadSetting);
        var pluginTypes = GetPluginsOfType<T>(assemblies);
        return LoadPlugins<T>(pluginTypes);
    }

    private static IEnumerable<T> LoadPlugins<T>(IEnumerable<Type> pluginTypes) where T : class
    {
        foreach (var t in pluginTypes)
        {
            T? activate;
            try { activate = (T?)Activator.CreateInstance(t); }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load plugin [{t.Name}]: {t.FullName}");
                Debug.WriteLine(ex.Message);
                continue;
            }
            if (activate is not null)
                yield return activate;
        }
    }

    private static IEnumerable<Assembly> GetAssemblies(IEnumerable<string> dllFileNames, PluginLoadSetting loadSetting)
    {
        var loadMethod = GetPluginLoadMethod(loadSetting);
        var result = new List<Assembly>();

        foreach (var file in dllFileNames)
        {
            try
            {
                var assembly = loadMethod(file);
                result.Add(assembly);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load plugin from file: {file}");
                Debug.WriteLine(ex.Message);
            }
        }

        if (loadSetting.IsMerged())
        {
            result.Add(Assembly.GetExecutingAssembly()); // load merged too

            // Load embedded DLLs directly from resources
            result.AddRange(LoadEmbeddedPlugins());
        }

        return result;
    }

    private static Func<string, Assembly> GetPluginLoadMethod(PluginLoadSetting pls) => pls switch
    {
        LoadFrom or LoadFromMerged => Assembly.LoadFrom,
        LoadFile or LoadFileMerged => Assembly.LoadFile,
        UnsafeLoadFrom or UnsafeMerged => Assembly.UnsafeLoadFrom,
        _ => throw new IndexOutOfRangeException($"PluginLoadSetting: {pls} method not defined."),
    };

    public static bool IsMerged(this PluginLoadSetting loadSetting) => loadSetting is LoadFromMerged or LoadFileMerged or UnsafeMerged;

    private static IEnumerable<Type> GetPluginsOfType<T>(IEnumerable<Assembly> assemblies)
    {
        var pluginType = typeof(T);
        return assemblies.SelectMany(z => GetPluginTypes(z, pluginType));
    }

    private static IEnumerable<Type> GetPluginTypes(Assembly z, Type plugin)
    {
        try
        {
            // Handle Costura merged plugin dll's; need to Attach for them to correctly retrieve their dependencies.
            var assemblyLoaderType = z.GetType("Costura.AssemblyLoader", false);
            var attachMethod = assemblyLoaderType?.GetMethod("Attach", BindingFlags.Static | BindingFlags.Public);
            attachMethod?.Invoke(null, []);

            var types = z.GetExportedTypes();
            return types.Where(type => IsTypePlugin(type, plugin));
        }
        // User plugins can be out of date, with mismatching API surfaces.
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to load plugin [{plugin.FullName}]: {z.FullName}");
            Debug.WriteLine(ex.Message);
            if (ex is not ReflectionTypeLoadException rtle)
                return [];

            foreach (var le in rtle.LoaderExceptions)
            {
                if (le is not null)
                    Debug.WriteLine(le.Message);
            }
            return [];
        }
    }

    private static List<Assembly> LoadEmbeddedPlugins()
    {
        var result = new List<Assembly>();
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        foreach (var resource in resources)
        {
            if (!resource.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream == null)
                    continue;

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                var loadedAssembly = Assembly.Load(bytes);
                result.Add(loadedAssembly);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load embedded plugin: {resource}");
                Debug.WriteLine(ex.Message);
            }
        }

        return result;
    }

    private static bool IsTypePlugin(Type type, Type plugin)
    {
        if (type.IsInterface || type.IsAbstract)
            return false;
        return plugin.IsAssignableFrom(type);
    }
}
