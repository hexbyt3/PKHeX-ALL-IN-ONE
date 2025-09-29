using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using PKHeX.Core;

namespace PKHeX.WinForms;

public sealed class SlotExportSettings
{
    [LocalizedDescription("Settings to use for box exports.")]
    public BoxExportSettings BoxExport { get; set; } = new();

    [LocalizedDescription("Selected File namer to use for box exports for the GUI, if multiple are available.")]
    public string DefaultBoxExportNamer { get; set; } = "";

    [LocalizedDescription("Allow drag and drop of boxdata binary files from the GUI via the Box tab.")]
    public bool AllowBoxDataDrop { get; set; } // default to false, clunky to use

    [LocalizedDescription("Selected File namer to use for individual PKM exports/saves, if multiple are available.")]
    [TypeConverter(typeof(PKMNamerTypeConverter))]
    public string DefaultPKMExportNamer { get; set; } = "Gengar Namer";
}

public sealed class PKMNamerTypeConverter : System.ComponentModel.StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

    public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
    {
        var names = EntityFileNamer.AvailableNamers.Select(n => n.Name).ToList();
        return new System.ComponentModel.TypeConverter.StandardValuesCollection(names);
    }
}
