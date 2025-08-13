using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using PKHeX.Core;

namespace PKHeX.WinForms;

public partial class SettingsEditor : Form
{
    public bool BlankChanged { get; private set; }
    private bool _themeChanged;
    private bool _originalThemeSetting;

    // Remember the last settings tab for the remainder of the session.
    private static string? Last;

    public SettingsEditor(object obj)
    {
        InitializeComponent();
        WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
        
        // Apply dark theme if enabled
        if (DarkTheme.IsEnabled)
        {
            DarkTheme.ApplyTheme(this);
        }
        
        // Store original theme setting
        if (obj is PKHeXSettings settings)
            _originalThemeSetting = settings.Display.UseDarkTheme;
            
        LoadSettings(obj);

        if (obj is PKHeXSettings s)
        {
            static bool IsInvalidSaveFileVersion(GameVersion value) => value is 0 or GameVersion.GO;
            CB_Blank.InitializeBinding();
            CB_Blank.DataSource = GameInfo.Sources.VersionDataSource.Where(z => !IsInvalidSaveFileVersion((GameVersion)z.Value)).ToList();
            CB_Blank.SelectedValue = (int)s.Startup.DefaultSaveVersion;
            CB_Blank.SelectedValueChanged += (_, _) =>
            {
                var index = WinFormsUtil.GetIndex(CB_Blank);
                var version = (GameVersion)index;
                if (IsInvalidSaveFileVersion(version))
                    return;
                s.Startup.DefaultSaveVersion = version;
            };
            CB_Blank.SelectedIndexChanged += (_, _) => BlankChanged = !IsInvalidSaveFileVersion((GameVersion)WinFormsUtil.GetIndex(CB_Blank));
            B_Reset.Click += (_, _) => DeleteSettings();
        }
        else
        {
            FLP_Blank.Visible = false;
            B_Reset.Visible = false;
        }

        if (Last is not null && tabControl1.Controls[Last] is TabPage tab)
            tabControl1.SelectedTab = tab;
        tabControl1.SelectedIndexChanged += (_, _) => Last = tabControl1.SelectedTab?.Name;

        this.CenterToForm(FindForm());
    }

    private void LoadSettings(object obj)
    {
        var type = obj.GetType();
        var props = ReflectUtil.GetPropertiesCanWritePublicDeclared(type)
            .Order();
        foreach (var p in props)
        {
            var state = ReflectUtil.GetValue(obj, p);
            if (state is null)
                continue;

            var tab = new TabPage(p) { Name = $"Tab_{p}" };
            var pg = new PropertyGrid { SelectedObject = state, Dock = DockStyle.Fill };
            
            // Apply dark theme to PropertyGrid if enabled
            if (DarkTheme.IsEnabled)
            {
                pg.ViewBackColor = Color.FromArgb(62, 62, 66);
                pg.ViewForeColor = Color.FromArgb(241, 241, 241);
                pg.ViewBorderColor = Color.FromArgb(85, 85, 85);
                pg.HelpBackColor = Color.FromArgb(45, 45, 48);
                pg.HelpForeColor = Color.FromArgb(241, 241, 241);
                pg.HelpBorderColor = Color.FromArgb(85, 85, 85);
                pg.CategoryForeColor = Color.FromArgb(241, 241, 241);
                pg.CategorySplitterColor = Color.FromArgb(85, 85, 85);
                pg.LineColor = Color.FromArgb(85, 85, 85);
                tab.BackColor = Color.FromArgb(30, 30, 30);
                tab.ForeColor = Color.FromArgb(241, 241, 241);
            }
            
            // Add property changed event handler for Display settings
            if (p == "Display" && state is DisplaySettings displaySettings)
            {
                pg.PropertyValueChanged += (sender, e) =>
                {
                    if (e.ChangedItem?.PropertyDescriptor?.Name == "UseDarkTheme")
                    {
                        _themeChanged = displaySettings.UseDarkTheme != _originalThemeSetting;
                    }
                };
            }
            
            tab.Controls.Add(pg);
            pg.ExpandAllGridItems();
            tabControl1.TabPages.Add(tab);
        }
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        
        if (_themeChanged && !e.Cancel)
        {
            var result = WinFormsUtil.Prompt(MessageBoxButtons.OK, 
                "Theme changes will take effect after restarting the application.", 
                "Please restart PKHeX to apply the theme changes.");
        }
    }

    private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
            Close();
    }

    private static void DeleteSettings()
    {
        try
        {
            var dr = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Resetting settings requires the program to exit.", MessageStrings.MsgContinue);
            if (dr != DialogResult.Yes)
                return;
            var path = Main.ConfigPath;
            if (File.Exists(path))
                File.Delete(path);
            System.Diagnostics.Process.Start(Application.ExecutablePath);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error("Failed to delete settings.", ex.Message);
        }
    }
}
