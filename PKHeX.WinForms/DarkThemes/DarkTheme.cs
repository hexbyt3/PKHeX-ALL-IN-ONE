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
    private static readonly Color SlotBackgroundColor = Color.FromArgb(70, 70, 70);
    private static readonly Color EmptySlotBackgroundColor = Color.FromArgb(85, 85, 85);
    private static readonly Color TextColor = Color.FromArgb(241, 241, 241);
    private static readonly Color BorderColor = Color.FromArgb(60, 60, 60);
    private static readonly Color HoverColor = Color.FromArgb(0, 122, 204);
    private static readonly Color SelectedColor = Color.FromArgb(76, 107, 135);
    private static readonly Color AccentColor = Color.FromArgb(0, 122, 204);
    private static readonly Color DisabledColor = Color.FromArgb(80, 80, 80);

    private static bool _initialized;
    private static readonly HashSet<IntPtr> ProcessedForms = [];
    private static Timer? _refreshTimer;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        foreach (Form form in Application.OpenForms)
        {
            ApplyTheme(form);
        }

        Application.Idle += OnApplicationIdle;

        _refreshTimer = new Timer
        {
            Interval = 500
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
        _refreshTimer.Start();
    }

    private static void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form != null && !form.IsDisposed && form.Visible)
                {
                    RefreshTheme(form);
                }
            }
        }
        catch
        {
        }
    }

    public static void RefreshTheme(Control control)
    {
        if (control == null || control.IsDisposed) return;

        if (NeedsRetheming(control))
        {
            ApplyControlTheme(control);
        }

        if (control is PictureBox pb && pb.BorderStyle != BorderStyle.None)
        {
            if (pb.BackColor != SlotBackgroundColor && pb.BackColor != EmptySlotBackgroundColor)
            {
                ApplyControlTheme(control);
            }
        }

        foreach (Control child in control.Controls)
        {
            RefreshTheme(child);
        }
    }

    private static bool NeedsRetheming(Control control)
    {
        if (control is ComboBox || control is TextBox || control is MaskedTextBox)
        {
            return control.BackColor == SystemColors.Window || control.BackColor == Color.White;
        }

        if (control is Panel || control is FlowLayoutPanel || control is TabPage)
        {
            return control.BackColor == SystemColors.Control || control.BackColor == Color.White ||
                   control.BackColor == SystemColors.Window;
        }

        if (control is PictureBox pb && pb.BorderStyle != BorderStyle.None)
        {
            return pb.BackColor == SystemColors.Control || pb.BackColor == Color.White ||
                   pb.BackColor == SystemColors.Window || pb.BackColor == Color.Transparent;
        }

        return false;
    }

    private static void OnApplicationIdle(object? sender, EventArgs e)
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
        }
    }

    public static void ApplyTheme(Control control)
    {
        if (control == null || control.IsDisposed) return;

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

    public static void ForceRefreshTheme()
    {
        foreach (Form form in Application.OpenForms)
        {
            if (form != null && !form.IsDisposed)
            {
                ApplyTheme(form);
            }
        }
    }

    public static void RefreshBoxSlots()
    {
        foreach (Form form in Application.OpenForms)
        {
            if (form != null && !form.IsDisposed)
            {
                RefreshSlotsInControl(form);
            }
        }
    }

    private static void RefreshSlotsInControl(Control control)
    {
        if (control is PictureBox pb)
        {
            bool isSlot = pb.BorderStyle != BorderStyle.None ||
                         pb.Name.Contains("bpkm") ||
                         pb.Name.Contains("slot") ||
                         pb.Parent?.Name.Contains("Box") == true ||
                         pb.Parent?.GetType().Name.Contains("Box") == true;

            if (isSlot)
            {
                ApplyControlTheme(pb);
            }
        }

        foreach (Control child in control.Controls)
        {
            RefreshSlotsInControl(child);
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
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = HoverColor;
            button.FlatAppearance.MouseDownBackColor = SelectedColor;

            if (!button.Enabled)
            {
                button.BackColor = DisabledColor;
            }
        }
        else if (control is TextBox textBox)
        {
            textBox.BackColor = ControlBackgroundColor;
            textBox.ForeColor = TextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is MaskedTextBox maskedTextBox)
        {
            maskedTextBox.BackColor = ControlBackgroundColor;
            maskedTextBox.ForeColor = TextColor;
            maskedTextBox.BorderStyle = BorderStyle.FixedSingle;
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
            if (label.BackColor == SystemColors.Control || label.Parent != null)
                label.BackColor = Color.Transparent;
        }
        else if (control is GroupBox groupBox)
        {
            groupBox.ForeColor = TextColor;
            groupBox.BackColor = BackgroundColor;
            groupBox.Paint -= GroupBox_Paint;
            groupBox.Paint += GroupBox_Paint;
        }
        else if (control is Panel panel)
        {
            panel.BackColor = BackgroundColor;
        }
        else if (control is FlowLayoutPanel flowPanel)
        {
            flowPanel.BackColor = BackgroundColor;
        }
        else if (control is TabControl tabControl)
        {
            tabControl.BackColor = BackgroundColor;
            tabControl.ForeColor = TextColor;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem -= TabControl_DrawItem;
            tabControl.DrawItem += TabControl_DrawItem;
        }
        else if (control is TabPage tabPage)
        {
            tabPage.BackColor = BackgroundColor;
            tabPage.ForeColor = TextColor;
            tabPage.UseVisualStyleBackColor = false;

            tabPage.Refresh();
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
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.BackColor = ControlBackgroundColor;
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = SelectedColor;
            dgv.DefaultCellStyle.SelectionForeColor = TextColor;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = SecondaryBackgroundColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = SecondaryBackgroundColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextColor;

            dgv.RowHeadersDefaultCellStyle.BackColor = SecondaryBackgroundColor;
            dgv.RowHeadersDefaultCellStyle.ForeColor = TextColor;
            dgv.RowHeadersDefaultCellStyle.SelectionBackColor = SelectedColor;
            dgv.RowHeadersDefaultCellStyle.SelectionForeColor = TextColor;
        }
        else if (control is PictureBox pictureBox)
        {
            bool isSlot = pictureBox.BorderStyle != BorderStyle.None ||
                         pictureBox.Name.Contains("bpkm") ||
                         pictureBox.Name.Contains("slot") ||
                         pictureBox.Parent?.Name.Contains("Box") == true ||
                         pictureBox.Parent?.GetType().Name.Contains("Box") == true;

            if (isSlot)
            {
                pictureBox.BackColor = SlotBackgroundColor;
                if (pictureBox.Image == null)
                {
                    pictureBox.BackColor = EmptySlotBackgroundColor;
                }
                if (pictureBox.BorderStyle == BorderStyle.None)
                {
                    pictureBox.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else if (pictureBox.BackColor == SystemColors.Control || pictureBox.BackColor == Color.Transparent)
            {
                pictureBox.BackColor = Color.Transparent;
            }
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.ForeColor = TextColor;
            if (checkBox.BackColor == SystemColors.Control)
                checkBox.BackColor = Color.Transparent;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.FlatAppearance.BorderColor = BorderColor;
            checkBox.FlatAppearance.CheckedBackColor = AccentColor;
            checkBox.FlatAppearance.MouseOverBackColor = HoverColor;
        }
        else if (control is RadioButton radioButton)
        {
            radioButton.ForeColor = TextColor;
            radioButton.BackColor = Color.Transparent;
            radioButton.FlatStyle = FlatStyle.Flat;
            radioButton.FlatAppearance.BorderColor = BorderColor;
            radioButton.FlatAppearance.CheckedBackColor = AccentColor;
            radioButton.FlatAppearance.MouseOverBackColor = HoverColor;
        }
        else if (control is NumericUpDown numericUpDown)
        {
            numericUpDown.BackColor = ControlBackgroundColor;
            numericUpDown.ForeColor = TextColor;
            numericUpDown.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is DateTimePicker dateTimePicker)
        {
            dateTimePicker.BackColor = ControlBackgroundColor;
            dateTimePicker.ForeColor = TextColor;
            dateTimePicker.CalendarMonthBackground = ControlBackgroundColor;
            dateTimePicker.CalendarForeColor = TextColor;
            dateTimePicker.CalendarTitleBackColor = SecondaryBackgroundColor;
            dateTimePicker.CalendarTitleForeColor = TextColor;
            dateTimePicker.CalendarTrailingForeColor = DisabledColor;
        }
        else if (control is SplitContainer splitContainer)
        {
            splitContainer.BackColor = BackgroundColor;
            splitContainer.Panel1.BackColor = BackgroundColor;
            splitContainer.Panel2.BackColor = BackgroundColor;
        }
        else if (control is StatusStrip statusStrip)
        {
            statusStrip.BackColor = SecondaryBackgroundColor;
            statusStrip.ForeColor = TextColor;
            statusStrip.Renderer = new DarkMenuRenderer();
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
            linkLabel.DisabledLinkColor = DisabledColor;
        }
        else
        {
            if (control.BackColor == SystemColors.Control || control.BackColor == SystemColors.Window)
                control.BackColor = BackgroundColor;
            if (control.ForeColor == SystemColors.ControlText || control.ForeColor == SystemColors.WindowText)
                control.ForeColor = TextColor;
        }

        ApplyCustomControlTheme(control);
    }

    private static void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tabControl = sender as TabControl;
        if (tabControl == null) return;

        var bounds = e.Bounds;
        var isSelected = e.Index == tabControl.SelectedIndex;

        using (var brush = new SolidBrush(isSelected ? SelectedColor : SecondaryBackgroundColor))
        {
            e.Graphics.FillRectangle(brush, bounds);
        }

        var text = tabControl.TabPages[e.Index].Text;
        var textBounds = new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 6);
        TextRenderer.DrawText(e.Graphics, text, e.Font, textBounds, TextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static void GroupBox_Paint(object? sender, PaintEventArgs e)
    {
        var groupBox = sender as GroupBox;
        if (groupBox == null) return;

        var textSize = TextRenderer.MeasureText(groupBox.Text, groupBox.Font);
        var borderRect = e.ClipRectangle;
        borderRect.Y += textSize.Height / 2;
        borderRect.Height -= textSize.Height / 2;

        ControlPaint.DrawBorder(e.Graphics, borderRect, BorderColor, ButtonBorderStyle.Solid);

        var textRect = new Rectangle(10, 0, textSize.Width, textSize.Height);
        e.Graphics.FillRectangle(new SolidBrush(groupBox.BackColor), textRect);
        TextRenderer.DrawText(e.Graphics, groupBox.Text, groupBox.Font, textRect, groupBox.ForeColor);
    }

    private static void ApplyCustomControlTheme(Control control)
    {
        var typeName = control.GetType().Name;

        switch (typeName)
        {
            case "PKMEditor":
            case "SAVEditor":
                control.BackColor = BackgroundColor;
                control.ForeColor = TextColor;
                break;

            case "SelectablePictureBox":
                if (control is PictureBox pb)
                {
                    pb.BackColor = SlotBackgroundColor;
                    if (pb.BorderStyle != BorderStyle.None)
                    {
                        pb.BorderStyle = BorderStyle.FixedSingle;
                    }
                }
                break;

            case "GenderToggle":
            case "TrainerID":
            case "MoveChoice":
            case "StatEditor":
            case "ContestStat":
            case "FormArgument":
            case "ShinyLeaf":
            case "CatchRate":
            case "SizeCP":
            case "StatusConditionView":
                control.BackColor = Color.Transparent;
                control.ForeColor = TextColor;
                break;

            case "BoxEditor":
                control.BackColor = BackgroundColor;
                control.ForeColor = TextColor;
                break;

            case "VerticalTabControlEntityEditor":
                control.BackColor = SecondaryBackgroundColor;
                control.ForeColor = TextColor;
                break;

            default:
                if (control.Name == "Hidden_TC" || control.Name.StartsWith("Hidden_") ||
                    control.Name.StartsWith("FLP_") || control.Name.StartsWith("Tab_"))
                {
                    control.BackColor = BackgroundColor;
                    control.ForeColor = TextColor;
                }
                else if (control is PictureBox slotPb &&
                    (control.Name.Contains("bpkm") || control.Name.Contains("slot") ||
                     control.Parent?.GetType().Name == "BoxEditor"))
                {
                    slotPb.BackColor = SlotBackgroundColor;
                    if (slotPb.BorderStyle != BorderStyle.None)
                    {
                        slotPb.BorderStyle = BorderStyle.FixedSingle;
                    }
                }
                break;
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
            var adjustedImage = AdjustImageForDarkTheme(item.Image);
            if (adjustedImage != null)
            {
                item.Image = adjustedImage;
                item.Image.Tag = "DarkThemeProcessed";
            }
        }

        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
            {
                ApplyToolStripItemTheme(dropDownItem);
            }
        }
        else if (item is ToolStripSeparator separator)
        {
            separator.ForeColor = BorderColor;
        }
    }

    private static Image? AdjustImageForDarkTheme(Image original)
    {
        if (original == null) return null;

        try
        {
            var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            {
                var attributes = new ImageAttributes();

                float[][] colorMatrixElements = [
                    [1.2f,  0,  0,  0, 0],
                    [0,  1.2f,  0,  0, 0],
                    [0,  0,  1.2f,  0, 0],
                    [0,  0,  0,  1, 0],
                    [0.1f, 0.1f, 0.1f, 0, 1]
                ];

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

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = TextColor;
            base.OnRenderItemText(e);
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
        public override Color ImageMarginGradientBegin => SecondaryBackgroundColor;
        public override Color ImageMarginGradientEnd => SecondaryBackgroundColor;
        public override Color ImageMarginGradientMiddle => SecondaryBackgroundColor;
    }
}
