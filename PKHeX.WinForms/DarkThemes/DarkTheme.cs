using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;
using System;

namespace PKHeX.WinForms;

public static class DarkTheme
{
    private static readonly Color BackgroundColor = Color.FromArgb(30, 30, 30);
    private static readonly Color SecondaryBackgroundColor = Color.FromArgb(45, 45, 48);
    private static readonly Color ControlBackgroundColor = Color.FromArgb(62, 62, 66); 
    private static readonly Color TextColor = Color.FromArgb(241, 241, 241);
    private static readonly Color BorderColor = Color.FromArgb(85, 85, 85);
    private static readonly Color HoverColor = Color.FromArgb(0, 122, 204);
    private static readonly Color SelectedColor = Color.FromArgb(76, 107, 135);
    private static readonly Color AccentColor = Color.FromArgb(0, 122, 204);

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        foreach (Form form in Application.OpenForms)
        {
            ApplyTheme(form);
        }

        Application.Idle += OnApplicationIdle;
    }

    private static readonly HashSet<IntPtr> ProcessedForms = new();

    private static void OnApplicationIdle(object sender, EventArgs e)
    {
        try
        {
            foreach (Form form in Application.OpenForms)
            {
                if (!ProcessedForms.Contains(form.Handle))
                {
                    ProcessedForms.Add(form.Handle);

                    if (form.InvokeRequired)
                    {
                        form.BeginInvoke(new Action(() =>
                        {
                            ApplyTheme(form);
                            form.FormClosed += (s, args) => ProcessedForms.Remove(form.Handle);
                        }));
                    }
                    else
                    {
                        ApplyTheme(form);
                        form.FormClosed += (s, args) => ProcessedForms.Remove(form.Handle);
                    }
                }
            }
        }
        catch
        {
            // Ignore any cross-thread exceptions during theme application
        }
    }

    public static void ApplyTheme(Control control)
    {
        if (control == null) return;

        if (control.InvokeRequired)
        {
            control.BeginInvoke(new Action(() => ApplyTheme(control)));
            return;
        }

        ApplyControlTheme(control);

        foreach (Control child in control.Controls)
        {
            ApplyTheme(child);
        }

        if (control is ToolStrip toolStrip)
        {
            ApplyToolStripTheme(toolStrip);
        }

        if (control is Form form)
        {
            form.Load += (s, e) => ApplyTheme(form);
        }
    }

    private static void ApplyControlTheme(Control control)
    {
        if (control is Form form)
        {
            form.BackColor = BackgroundColor;
            form.ForeColor = TextColor;
        }
        else if (control is MenuStrip menuStrip)
        {
            menuStrip.BackColor = SecondaryBackgroundColor;
            menuStrip.ForeColor = TextColor;
            menuStrip.Renderer = new DarkMenuRenderer();
        }
        else if (control is Button button)
        {
            button.BackColor = SecondaryBackgroundColor;
            button.ForeColor = TextColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = HoverColor;
            button.FlatAppearance.MouseDownBackColor = SelectedColor;
        }
        else if (control is TextBox textBox)
        {
            textBox.BackColor = ControlBackgroundColor;
            textBox.ForeColor = TextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox comboBox)
        {
            comboBox.BackColor = ControlBackgroundColor;
            comboBox.ForeColor = TextColor;
            comboBox.FlatStyle = FlatStyle.Flat;
        }
        else if (control is Label label)
        {
            label.ForeColor = TextColor;
            if (label.BackColor == SystemColors.Control)
                label.BackColor = Color.Transparent;
        }
        else if (control is GroupBox groupBox)
        {
            groupBox.ForeColor = TextColor;
            groupBox.BackColor = BackgroundColor;
        }
        else if (control is Panel panel)
        {
            panel.BackColor = BackgroundColor;
        }
        else if (control is TabControl tabControl)
        {
            tabControl.BackColor = BackgroundColor;
            tabControl.ForeColor = TextColor;
        }
        else if (control is TabPage tabPage)
        {
            tabPage.BackColor = BackgroundColor;
            tabPage.ForeColor = TextColor;
        }
        else if (control is ListBox listBox)
        {
            listBox.BackColor = ControlBackgroundColor;
            listBox.ForeColor = TextColor;
            listBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ListView listView)
        {
            listView.BackColor = ControlBackgroundColor;
            listView.ForeColor = TextColor;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is DataGridView dgv)
        {
            dgv.BackgroundColor = ControlBackgroundColor;
            dgv.GridColor = BorderColor;
            dgv.DefaultCellStyle.BackColor = ControlBackgroundColor;
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = SelectedColor;
            dgv.DefaultCellStyle.SelectionForeColor = TextColor;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = SecondaryBackgroundColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
        }
        else if (control is PictureBox pictureBox)
        {
            if (pictureBox.BackColor == SystemColors.Control || pictureBox.BackColor == Color.Transparent)
                pictureBox.BackColor = Color.Transparent;
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.ForeColor = TextColor;
            checkBox.BackColor = BackgroundColor;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.FlatAppearance.BorderColor = BorderColor;
            checkBox.FlatAppearance.CheckedBackColor = AccentColor;
            checkBox.FlatAppearance.MouseOverBackColor = HoverColor;
        }
        else if (control is RadioButton radioButton)
        {
            radioButton.ForeColor = TextColor;
            radioButton.BackColor = Color.Transparent;
        }
        else if (control is NumericUpDown numericUpDown)
        {
            numericUpDown.BackColor = ControlBackgroundColor;
            numericUpDown.ForeColor = TextColor;
            numericUpDown.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is SplitContainer splitContainer)
        {
            splitContainer.BackColor = BackgroundColor;
        }
        else if (control is StatusStrip statusStrip)
        {
            statusStrip.BackColor = SecondaryBackgroundColor;
            statusStrip.ForeColor = TextColor;
        }
        else if (control is ContextMenuStrip contextMenu)
        {
            contextMenu.BackColor = SecondaryBackgroundColor;
            contextMenu.ForeColor = TextColor;
            contextMenu.Renderer = new DarkMenuRenderer();
        }
        else if (control is TreeView treeView)
        {
            treeView.BackColor = ControlBackgroundColor;
            treeView.ForeColor = TextColor;
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.LineColor = BorderColor;
        }
        else if (control is ToolStrip toolStrip)
        {
            toolStrip.BackColor = SecondaryBackgroundColor;
            toolStrip.ForeColor = TextColor;
            toolStrip.Renderer = new DarkMenuRenderer();
        }
        else if (control is LinkLabel linkLabel)
        {
            linkLabel.LinkColor = AccentColor;
            linkLabel.ActiveLinkColor = Color.FromArgb(142, 212, 254);
            linkLabel.VisitedLinkColor = AccentColor;
            linkLabel.BackColor = Color.Transparent;
        }

        ApplyCustomControlTheme(control);
    }

    private static void ApplyCustomControlTheme(Control control)
    {
        var typeName = control.GetType().Name;

        if (typeName == "PKMEditor" || typeName == "SAVEditor")
        {
            control.BackColor = BackgroundColor;
            control.ForeColor = TextColor;
        }
        else if (typeName == "SelectablePictureBox")
        {
            if (control is PictureBox pb && pb.BackColor == SystemColors.Control)
                pb.BackColor = ControlBackgroundColor;
        }
    }

    private static void ApplyToolStripTheme(ToolStrip toolStrip)
    {
        foreach (ToolStripItem item in toolStrip.Items)
        {
            ApplyToolStripItemTheme(item);
        }
    }

    private static void ApplyToolStripItemTheme(ToolStripItem item)
    {
        item.BackColor = SecondaryBackgroundColor;
        item.ForeColor = TextColor;

        if (item.Image != null && item.Image.Tag?.ToString() != "DarkThemeProcessed")
        {
            item.Image = AdjustImageForDarkTheme(item.Image);
            item.Image.Tag = "DarkThemeProcessed";
        }

        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
            {
                ApplyToolStripItemTheme(dropDownItem);
            }
        }
    }

    private static Image AdjustImageForDarkTheme(Image original)
    {
        if (original == null) return null;

        try
        {
            var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            {
                var attributes = new ImageAttributes();

                float[][] colorMatrixElements = {
                    new float[] {1.2f,  0,  0,  0, 0},
                    new float[] {0,  1.2f,  0,  0, 0},
                    new float[] {0,  0,  1.2f,  0, 0},
                    new float[] {0,  0,  0,  1, 0},
                    new float[] {0.1f, 0.1f, 0.1f, 0, 1}
                };

                var colorMatrix = new ColorMatrix(colorMatrixElements);
                attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return bitmap;
        }
        catch
        {
            return original;
        }
    }

    private class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            var g = e.Graphics;

            if (item.Selected || item.Pressed)
            {
                using var brush = new SolidBrush(HoverColor);
                g.FillRectangle(brush, new Rectangle(Point.Empty, item.Size));
            }
            else
            {
                using var brush = new SolidBrush(SecondaryBackgroundColor);
                g.FillRectangle(brush, new Rectangle(Point.Empty, item.Size));
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            using var pen = new Pen(BorderColor);
            g.DrawLine(pen, bounds.Left + 20, bounds.Height / 2, bounds.Right - 20, bounds.Height / 2);
        }
    }

    private class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => BorderColor;
        public override Color MenuItemBorder => BorderColor;
        public override Color MenuItemSelected => HoverColor;
        public override Color MenuItemSelectedGradientBegin => HoverColor;
        public override Color MenuItemSelectedGradientEnd => HoverColor;
        public override Color MenuItemPressedGradientBegin => SelectedColor;
        public override Color MenuItemPressedGradientEnd => SelectedColor;
        public override Color MenuStripGradientBegin => SecondaryBackgroundColor;
        public override Color MenuStripGradientEnd => SecondaryBackgroundColor;
        public override Color ToolStripDropDownBackground => SecondaryBackgroundColor;
        public override Color ToolStripGradientBegin => SecondaryBackgroundColor;
        public override Color ToolStripGradientEnd => SecondaryBackgroundColor;
        public override Color ToolStripGradientMiddle => SecondaryBackgroundColor;
    }
}
