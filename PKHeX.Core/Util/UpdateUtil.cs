using System;

namespace PKHeX.Core;

public static class UpdateUtil
{
    /// <summary>
    /// Gets the latest version of PKHeX according to the GitHub API
    /// </summary>
    /// <returns>A version representing the latest available version of PKHeX, or null if the latest version could not be determined</returns>
    public static Version? GetLatestPKHeXVersion()
    {
        const string apiEndpoint = "https://api.github.com/repos/hexbyt3/PKHeX-ALL-IN-ONE/releases/latest";
        var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
        if (responseJson is null)
            return null;

        // Parse it manually; no need to parse the entire json to object.
        const string tag = "tag_name";
        var index = responseJson.IndexOf(tag, StringComparison.Ordinal);
        if (index == -1)
            return null;

        var first = responseJson.IndexOf('"', index + tag.Length + 1) + 1;
        if (first == 0)
            return null;
        var second = responseJson.IndexOf('"', first);
        if (second == -1)
            return null;

        var tagString = responseJson.AsSpan()[first..second];
        
        // Extract base version from tag (remove v prefix and -rev.X suffix)
        var versionStr = tagString.ToString();
        if (versionStr.StartsWith("v"))
            versionStr = versionStr.Substring(1);
            
        // Remove revision suffix if present
        var revIndex = versionStr.IndexOf("-rev.", StringComparison.Ordinal);
        if (revIndex != -1)
            versionStr = versionStr.Substring(0, revIndex);
            
        return !Version.TryParse(versionStr, out var latestVersion) ? null : latestVersion;
    }
    
    /// <summary>
    /// Gets the latest release tag including revision suffix
    /// </summary>
    /// <returns>The latest release tag, or null if not found</returns>
    public static string? GetLatestReleaseTag()
    {
        const string apiEndpoint = "https://api.github.com/repos/hexbyt3/PKHeX-ALL-IN-ONE/releases/latest";
        var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
        if (responseJson is null)
            return null;

        // Parse the tag_name field
        const string tag = "tag_name";
        var index = responseJson.IndexOf(tag, StringComparison.Ordinal);
        if (index == -1)
            return null;

        var first = responseJson.IndexOf('"', index + tag.Length + 1) + 1;
        if (first == 0)
            return null;
        var second = responseJson.IndexOf('"', first);
        if (second == -1)
            return null;

        return responseJson.Substring(first, second - first);
    }
}
