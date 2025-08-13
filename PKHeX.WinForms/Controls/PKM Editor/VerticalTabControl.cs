using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls;

public class VerticalTabControl : TabControl
{
    protected static readonly Color DarkBackgroundColor = Color.FromArgb(30, 30, 30);
    protected static readonly Color DarkTabBackgroundColor = Color.FromArgb(45, 45, 48);
    protected static readonly Color DarkSelectedTabColor = Color.FromArgb(62, 62, 66);
    protected static readonly Color DarkTextColor = Color.FromArgb(241, 241, 241);
    protected static readonly Color DarkBorderColor = Color.FromArgb(85, 85, 85);
    protected static readonly Color DarkHoverColor = Color.FromArgb(0, 122, 204);

    protected bool UseDarkTheme => DarkTheme.IsEnabled;

    public VerticalTabControl()
    {
        Alignment = TabAlignment.Right;
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;

        if (UseDarkTheme)
        {
            BackColor = DarkBackgroundColor;
            ForeColor = DarkTextColor;
        }
    }
    
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (UseDarkTheme)
        {
            // Apply dark theme to all existing tab pages
            foreach (TabPage tab in TabPages)
            {
                tab.BackColor = DarkBackgroundColor;
                tab.ForeColor = DarkTextColor;
                tab.UseVisualStyleBackColor = false;
                ApplyThemeToControls(tab);
            }
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (UseDarkTheme && m.Msg == 0x0F) // WM_PAINT
        {
            using (var g = Graphics.FromHwnd(Handle))
            {
                var displayRect = DisplayRectangle;
                using (var brush = new SolidBrush(DarkBackgroundColor))
                {
                    g.FillRectangle(brush, displayRect);
                }
            }
        }
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var index = e.Index;
        if ((uint)index >= TabPages.Count)
            return;
        var bounds = GetTabRect(index);

        var graphics = e.Graphics;
        if (e.State == DrawItemState.Selected)
        {
            if (UseDarkTheme)
            {
                using var brush = new LinearGradientBrush(bounds, DarkSelectedTabColor, DarkTabBackgroundColor, 90f);
                graphics.FillRectangle(brush, bounds);

                using var highlightPen = new Pen(DarkHoverColor, 2);
                graphics.DrawLine(highlightPen, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
            }
            else
            {
                using var brush = new LinearGradientBrush(bounds, Color.White, Color.LightGray, 90f);
                graphics.FillRectangle(brush, bounds);
            }
        }
        else
        {
            if (UseDarkTheme)
            {
                using var brush = new SolidBrush(DarkTabBackgroundColor);
                graphics.FillRectangle(brush, bounds);
            }
            else
            {
                e.DrawBackground();
            }
        }

        using var flags = new StringFormat();
        flags.Alignment = StringAlignment.Center;
        flags.LineAlignment = StringAlignment.Center;
        
        var textColor = UseDarkTheme ? DarkTextColor : ForeColor;
        using var text = new SolidBrush(textColor);
        var tab = TabPages[index];
        graphics.DrawString(tab.Text, Font, text, bounds, flags);

        if (UseDarkTheme && tab.BackColor != DarkBackgroundColor)
        {
            tab.BackColor = DarkBackgroundColor;
            tab.ForeColor = DarkTextColor;
        }
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage tabPage)
        {
            if (UseDarkTheme)
            {
                tabPage.BackColor = DarkBackgroundColor;
                tabPage.ForeColor = DarkTextColor;
                tabPage.UseVisualStyleBackColor = false;

                ApplyThemeToControls(tabPage);
            }
            else
            {
                tabPage.UseVisualStyleBackColor = true;
            }
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);

        if (UseDarkTheme && SelectedTab != null)
        {
            SelectedTab.BackColor = DarkBackgroundColor;
            SelectedTab.ForeColor = DarkTextColor;
            SelectedTab.UseVisualStyleBackColor = false;
            ApplyThemeToControls(SelectedTab);
        }
    }

    private void ApplyThemeToControls(Control parent)
    {
        if (!UseDarkTheme)
            return;

        foreach (Control control in parent.Controls)
        {
            if (control is Panel || control is GroupBox)
            {
                control.BackColor = DarkBackgroundColor;
                control.ForeColor = DarkTextColor;
            }

            if (control.HasChildren)
            {
                ApplyThemeToControls(control);
            }
        }
    }

    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        base.ScaleControl(factor, specified);
        ItemSize = new((int)(ItemSize.Width * factor.Width), (int)(ItemSize.Height * factor.Height));
    }
}

public sealed class VerticalTabControlEntityEditor : VerticalTabControl
{
    private static readonly Color[] SelectedTags =
    [
        Color.FromArgb(248, 152, 096),
        Color.FromArgb(128, 152, 248),
        Color.FromArgb(248, 168, 208),
        Color.FromArgb(112, 224, 112),
        Color.FromArgb(248, 240, 056),
        Color.FromArgb(188, 143, 143),
    ];

    public VerticalTabControlEntityEditor()
    {
        // Always set custom paint style to support both themes
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Always paint background based on theme
        var bgColor = UseDarkTheme ? DarkBackgroundColor : SystemColors.Control;
        using (var bgBrush = new SolidBrush(bgColor))
        {
            e.Graphics.FillRectangle(bgBrush, ClientRectangle);
        }

        var displayRect = DisplayRectangle;
        using (var displayBrush = new SolidBrush(bgColor))
        {
            e.Graphics.FillRectangle(displayBrush, displayRect);
        }

        // Draw all tabs
        for (int i = 0; i < TabCount; i++)
        {
            DrawItemEventArgs args = new DrawItemEventArgs(e.Graphics, Font, GetTabRect(i), i,
                i == SelectedIndex ? DrawItemState.Selected : DrawItemState.None);
            OnDrawItem(args);
        }

        // Draw border for selected tab
        if (SelectedTab != null)
        {
            var borderColor = UseDarkTheme ? DarkBorderColor : SystemColors.ControlDark;
            var tabRect = GetTabRect(SelectedIndex);
            var borderRect = new Rectangle(displayRect.Left - 1, displayRect.Top - 1,
                displayRect.Width + 1, displayRect.Height + 1);
            using (var borderPen = new Pen(borderColor, 1))
            {
                e.Graphics.DrawRectangle(borderPen, borderRect);
            }
        }
        
        // Ensure selected tab page has correct background
        if (SelectedTab != null && UseDarkTheme)
        {
            SelectedTab.BackColor = DarkBackgroundColor;
            SelectedTab.ForeColor = DarkTextColor;
            SelectedTab.UseVisualStyleBackColor = false;
        }
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var index = e.Index;
        if ((uint)index >= TabPages.Count)
            return;
        var bounds = GetTabRect(index);

        var graphics = e.Graphics;
        if (e.State == DrawItemState.Selected)
        {
            Color c1, c2;
            
            if (UseDarkTheme)
            {
                if (Main.Settings?.Draw != null)
                {
                    var settings = Main.Settings.Draw;
                    c1 = settings.VerticalSelectPrimary;
                    c2 = settings.VerticalSelectSecondary;

                    if (c1 == Color.White || c1.GetBrightness() > 0.8f)
                        c1 = DarkSelectedTabColor;
                    if (c2 == Color.LightGray || c2.GetBrightness() > 0.8f)
                        c2 = DarkTabBackgroundColor;
                }
                else
                {
                    c1 = DarkSelectedTabColor;
                    c2 = DarkTabBackgroundColor;
                }
            }
            else
            {
                // Light theme colors
                if (Main.Settings?.Draw != null)
                {
                    var settings = Main.Settings.Draw;
                    c1 = settings.VerticalSelectPrimary;
                    c2 = settings.VerticalSelectSecondary;
                    
                    // Don't override if custom colors are set that work for light theme
                    if (c1 == Color.White)
                        c1 = Color.FromArgb(255, 255, 255);
                    if (c2 == Color.LightGray)
                        c2 = Color.FromArgb(240, 240, 240);
                }
                else
                {
                    c1 = Color.White;
                    c2 = Color.LightGray;
                }
            }

            using var brush = new LinearGradientBrush(bounds, c1, c2, 90f);
            graphics.FillRectangle(brush, bounds);

            using var pipBrush = new SolidBrush(SelectedTags[index]);
            var pip = GetTabRect(index) with { Width = bounds.Width / 8 };
            graphics.FillRectangle(pipBrush, pip);

            using var pipBorder = new Pen(Color.FromArgb(100, 0, 0, 0), 1);
            graphics.DrawRectangle(pipBorder, pip.X, pip.Y, pip.Width - 1, pip.Height - 1);

            bounds = bounds with { Width = bounds.Width - pip.Width, X = bounds.X + pip.Width };
        }
        else
        {
            if (UseDarkTheme)
            {
                using var brush = new SolidBrush(DarkTabBackgroundColor);
                graphics.FillRectangle(brush, bounds);

                var mousePos = PointToClient(MousePosition);
                if (bounds.Contains(mousePos))
                {
                    using var hoverBrush = new SolidBrush(Color.FromArgb(50, DarkHoverColor));
                    graphics.FillRectangle(hoverBrush, bounds);
                }
            }
            else
            {
                // Light theme - ensure background is visible
                using var brush = new SolidBrush(SystemColors.Control);
                graphics.FillRectangle(brush, bounds);
            }
        }

        using var flags = new StringFormat();
        flags.Alignment = StringAlignment.Center;
        flags.LineAlignment = StringAlignment.Center;
        
        var textColor = UseDarkTheme ? DarkTextColor : SystemColors.ControlText;
        using var text = new SolidBrush(textColor);
        var tab = TabPages[index];
        graphics.DrawString(tab.Text, Font, text, bounds, flags);
    }
}