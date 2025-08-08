namespace PKHeX.WinForms
{
    partial class SAV_LivingDexBuilder
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            GB_Options = new System.Windows.Forms.GroupBox();
            L_SaveFileInfo = new System.Windows.Forms.Label();
            CHK_OverwriteExisting = new System.Windows.Forms.CheckBox();
            NUD_StartBox = new System.Windows.Forms.NumericUpDown();
            L_StartBox = new System.Windows.Forms.Label();
            CHK_LegalOnly = new System.Windows.Forms.CheckBox();
            CHK_MysteryGifts = new System.Windows.Forms.CheckBox();
            CHK_Gender = new System.Windows.Forms.CheckBox();
            CHK_Forms = new System.Windows.Forms.CheckBox();
            CHK_Shiny = new System.Windows.Forms.CheckBox();
            CHK_MinLevel = new System.Windows.Forms.CheckBox();
            CB_Sorting = new System.Windows.Forms.ComboBox();
            L_Sorting = new System.Windows.Forms.Label();
            GB_Missing = new System.Windows.Forms.GroupBox();
            LB_Missing = new System.Windows.Forms.ListBox();
            L_Status = new System.Windows.Forms.Label();
            B_Generate = new System.Windows.Forms.Button();
            B_Import = new System.Windows.Forms.Button();
            B_Cancel = new System.Windows.Forms.Button();
            PB_Progress = new System.Windows.Forms.ProgressBar();
            L_GeneratedCount = new System.Windows.Forms.Label();
            GB_Options.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_StartBox).BeginInit();
            GB_Missing.SuspendLayout();
            SuspendLayout();
            // 
            // GB_Options
            // 
            GB_Options.Controls.Add(L_SaveFileInfo);
            GB_Options.Controls.Add(CHK_OverwriteExisting);
            GB_Options.Controls.Add(NUD_StartBox);
            GB_Options.Controls.Add(L_StartBox);
            GB_Options.Controls.Add(CHK_LegalOnly);
            GB_Options.Controls.Add(CHK_MysteryGifts);
            GB_Options.Controls.Add(CHK_Gender);
            GB_Options.Controls.Add(CHK_Forms);
            GB_Options.Controls.Add(CHK_Shiny);
            GB_Options.Controls.Add(CHK_MinLevel);
            GB_Options.Controls.Add(CB_Sorting);
            GB_Options.Controls.Add(L_Sorting);
            GB_Options.Location = new System.Drawing.Point(12, 12);
            GB_Options.Name = "GB_Options";
            GB_Options.Size = new System.Drawing.Size(260, 243);
            GB_Options.TabIndex = 0;
            GB_Options.TabStop = false;
            GB_Options.Text = "Options";
            // 
            // L_SaveFileInfo
            // 
            L_SaveFileInfo.AutoSize = true;
            L_SaveFileInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            L_SaveFileInfo.Location = new System.Drawing.Point(10, 20);
            L_SaveFileInfo.Name = "L_SaveFileInfo";
            L_SaveFileInfo.Size = new System.Drawing.Size(100, 13);
            L_SaveFileInfo.TabIndex = 0;
            L_SaveFileInfo.Text = "Game: Version";
            // 
            // CHK_OverwriteExisting
            // 
            CHK_OverwriteExisting.AutoSize = true;
            CHK_OverwriteExisting.Location = new System.Drawing.Point(10, 213);
            CHK_OverwriteExisting.Name = "CHK_OverwriteExisting";
            CHK_OverwriteExisting.Size = new System.Drawing.Size(152, 17);
            CHK_OverwriteExisting.TabIndex = 10;
            CHK_OverwriteExisting.Text = "Overwrite Existing Pokémon";
            CHK_OverwriteExisting.UseVisualStyleBackColor = true;
            // 
            // NUD_StartBox
            // 
            NUD_StartBox.Location = new System.Drawing.Point(100, 187);
            NUD_StartBox.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
            NUD_StartBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            NUD_StartBox.Name = "NUD_StartBox";
            NUD_StartBox.Size = new System.Drawing.Size(50, 20);
            NUD_StartBox.TabIndex = 9;
            NUD_StartBox.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // L_StartBox
            // 
            L_StartBox.AutoSize = true;
            L_StartBox.Location = new System.Drawing.Point(10, 189);
            L_StartBox.Name = "L_StartBox";
            L_StartBox.Size = new System.Drawing.Size(52, 13);
            L_StartBox.TabIndex = 8;
            L_StartBox.Text = "Start Box:";
            // 
            // CHK_LegalOnly
            // 
            CHK_LegalOnly.AutoSize = true;
            CHK_LegalOnly.Checked = true;
            CHK_LegalOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            CHK_LegalOnly.Location = new System.Drawing.Point(10, 163);
            CHK_LegalOnly.Name = "CHK_LegalOnly";
            CHK_LegalOnly.Size = new System.Drawing.Size(75, 17);
            CHK_LegalOnly.TabIndex = 9;
            CHK_LegalOnly.Text = "Legal Only";
            CHK_LegalOnly.UseVisualStyleBackColor = true;
            // 
            // CHK_MysteryGifts
            // 
            CHK_MysteryGifts.AutoSize = true;
            CHK_MysteryGifts.Location = new System.Drawing.Point(10, 140);
            CHK_MysteryGifts.Name = "CHK_MysteryGifts";
            CHK_MysteryGifts.Size = new System.Drawing.Size(135, 17);
            CHK_MysteryGifts.TabIndex = 8;
            CHK_MysteryGifts.Text = "Include Mystery Gifts";
            CHK_MysteryGifts.UseVisualStyleBackColor = true;
            // 
            // CHK_Gender
            // 
            CHK_Gender.AutoSize = true;
            CHK_Gender.Checked = true;
            CHK_Gender.CheckState = System.Windows.Forms.CheckState.Checked;
            CHK_Gender.Location = new System.Drawing.Point(10, 117);
            CHK_Gender.Name = "CHK_Gender";
            CHK_Gender.Size = new System.Drawing.Size(140, 17);
            CHK_Gender.TabIndex = 7;
            CHK_Gender.Text = "Include Both Genders";
            CHK_Gender.UseVisualStyleBackColor = true;
            CHK_Gender.CheckedChanged += FilterChanged;
            // 
            // CHK_Forms
            // 
            CHK_Forms.AutoSize = true;
            CHK_Forms.Checked = true;
            CHK_Forms.CheckState = System.Windows.Forms.CheckState.Checked;
            CHK_Forms.Location = new System.Drawing.Point(10, 94);
            CHK_Forms.Name = "CHK_Forms";
            CHK_Forms.Size = new System.Drawing.Size(111, 17);
            CHK_Forms.TabIndex = 6;
            CHK_Forms.Text = "Include All Forms";
            CHK_Forms.UseVisualStyleBackColor = true;
            CHK_Forms.CheckedChanged += FilterChanged;
            // 
            // CHK_Shiny
            // 
            CHK_Shiny.AutoSize = true;
            CHK_Shiny.Location = new System.Drawing.Point(10, 71);
            CHK_Shiny.Name = "CHK_Shiny";
            CHK_Shiny.Size = new System.Drawing.Size(98, 17);
            CHK_Shiny.TabIndex = 4;
            CHK_Shiny.Text = "Generate Shiny";
            CHK_Shiny.UseVisualStyleBackColor = true;
            // 
            // CHK_MinLevel
            // 
            CHK_MinLevel.AutoSize = true;
            CHK_MinLevel.Location = new System.Drawing.Point(120, 71);
            CHK_MinLevel.Name = "CHK_MinLevel";
            CHK_MinLevel.Size = new System.Drawing.Size(100, 17);
            CHK_MinLevel.TabIndex = 5;
            CHK_MinLevel.Text = "Minimum Level";
            CHK_MinLevel.UseVisualStyleBackColor = true;
            // 
            // CB_Sorting
            // 
            CB_Sorting.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Sorting.FormattingEnabled = true;
            CB_Sorting.Location = new System.Drawing.Point(100, 43);
            CB_Sorting.Name = "CB_Sorting";
            CB_Sorting.Size = new System.Drawing.Size(150, 21);
            CB_Sorting.TabIndex = 3;
            CB_Sorting.SelectedIndexChanged += FilterChanged;
            // 
            // L_Sorting
            // 
            L_Sorting.AutoSize = true;
            L_Sorting.Location = new System.Drawing.Point(10, 46);
            L_Sorting.Name = "L_Sorting";
            L_Sorting.Size = new System.Drawing.Size(43, 13);
            L_Sorting.TabIndex = 2;
            L_Sorting.Text = "Sort By:";
            // 
            // 
            // GB_Missing
            // 
            GB_Missing.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            GB_Missing.Controls.Add(LB_Missing);
            GB_Missing.Location = new System.Drawing.Point(278, 12);
            GB_Missing.Name = "GB_Missing";
            GB_Missing.Size = new System.Drawing.Size(344, 379);
            GB_Missing.TabIndex = 1;
            GB_Missing.TabStop = false;
            GB_Missing.Text = "Missing Entries";
            // 
            // LB_Missing
            // 
            LB_Missing.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            LB_Missing.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            LB_Missing.FormattingEnabled = true;
            LB_Missing.ItemHeight = 14;
            LB_Missing.Location = new System.Drawing.Point(6, 19);
            LB_Missing.Name = "LB_Missing";
            LB_Missing.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            LB_Missing.Size = new System.Drawing.Size(332, 354);
            LB_Missing.TabIndex = 0;
            // 
            // L_Status
            // 
            L_Status.AutoSize = true;
            L_Status.Location = new System.Drawing.Point(12, 267);
            L_Status.Name = "L_Status";
            L_Status.Size = new System.Drawing.Size(93, 13);
            L_Status.TabIndex = 2;
            L_Status.Text = "Missing: 0 entries";
            // 
            // B_Generate
            // 
            B_Generate.Location = new System.Drawing.Point(12, 300);
            B_Generate.Name = "B_Generate";
            B_Generate.Size = new System.Drawing.Size(120, 30);
            B_Generate.TabIndex = 3;
            B_Generate.Text = "Generate Pokémon";
            B_Generate.UseVisualStyleBackColor = true;
            B_Generate.Click += B_Generate_Click;
            // 
            // B_Import
            // 
            B_Import.Enabled = false;
            B_Import.Location = new System.Drawing.Point(152, 300);
            B_Import.Name = "B_Import";
            B_Import.Size = new System.Drawing.Size(120, 30);
            B_Import.TabIndex = 4;
            B_Import.Text = "Import to Boxes";
            B_Import.UseVisualStyleBackColor = true;
            B_Import.Click += B_Import_Click;
            // 
            // B_Cancel
            // 
            B_Cancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            B_Cancel.Location = new System.Drawing.Point(12, 361);
            B_Cancel.Name = "B_Cancel";
            B_Cancel.Size = new System.Drawing.Size(75, 30);
            B_Cancel.TabIndex = 5;
            B_Cancel.Text = "Cancel";
            B_Cancel.UseVisualStyleBackColor = true;
            B_Cancel.Click += B_Cancel_Click;
            // 
            // PB_Progress
            // 
            PB_Progress.Location = new System.Drawing.Point(12, 336);
            PB_Progress.Name = "PB_Progress";
            PB_Progress.Size = new System.Drawing.Size(260, 10);
            PB_Progress.TabIndex = 6;
            PB_Progress.Visible = false;
            // 
            // L_GeneratedCount
            // 
            L_GeneratedCount.AutoSize = true;
            L_GeneratedCount.Location = new System.Drawing.Point(12, 293);
            L_GeneratedCount.Name = "L_GeneratedCount";
            L_GeneratedCount.Size = new System.Drawing.Size(120, 13);
            L_GeneratedCount.TabIndex = 7;
            L_GeneratedCount.Text = "Generated: 0 Pokémon";
            // 
            // SAV_LivingDexBuilder
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(634, 403);
            Controls.Add(L_GeneratedCount);
            Controls.Add(PB_Progress);
            Controls.Add(B_Cancel);
            Controls.Add(B_Import);
            Controls.Add(B_Generate);
            Controls.Add(L_Status);
            Controls.Add(GB_Missing);
            Controls.Add(GB_Options);
            MinimumSize = new System.Drawing.Size(650, 442);
            Name = "SAV_LivingDexBuilder";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Living Dex Builder";
            GB_Options.ResumeLayout(false);
            GB_Options.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_StartBox).EndInit();
            GB_Missing.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox GB_Options;
        private System.Windows.Forms.Label L_SaveFileInfo;
        private System.Windows.Forms.ComboBox CB_Sorting;
        private System.Windows.Forms.Label L_Sorting;
        private System.Windows.Forms.CheckBox CHK_Shiny;
        private System.Windows.Forms.CheckBox CHK_MinLevel;
        private System.Windows.Forms.CheckBox CHK_Forms;
        private System.Windows.Forms.CheckBox CHK_Gender;
        private System.Windows.Forms.CheckBox CHK_LegalOnly;
        private System.Windows.Forms.CheckBox CHK_MysteryGifts;
        private System.Windows.Forms.Label L_StartBox;
        private System.Windows.Forms.NumericUpDown NUD_StartBox;
        private System.Windows.Forms.CheckBox CHK_OverwriteExisting;
        private System.Windows.Forms.GroupBox GB_Missing;
        private System.Windows.Forms.ListBox LB_Missing;
        private System.Windows.Forms.Label L_Status;
        private System.Windows.Forms.Button B_Generate;
        private System.Windows.Forms.Button B_Import;
        private System.Windows.Forms.Button B_Cancel;
        private System.Windows.Forms.ProgressBar PB_Progress;
        private System.Windows.Forms.Label L_GeneratedCount;
    }
}