namespace VirtualRadar.Plugin.TileServerMBTiles.WinForms
{
    partial class OptionsView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.checkBoxEnabled = new System.Windows.Forms.CheckBox();
            this.labelFolder = new System.Windows.Forms.Label();
            this.textBoxFolder = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.checkBoxIsTms = new System.Windows.Forms.CheckBox();
            this.labelInfo = new System.Windows.Forms.Label();
            this.labelFileCount = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // checkBoxEnabled
            //
            this.checkBoxEnabled.AutoSize = true;
            this.checkBoxEnabled.Location = new System.Drawing.Point(12, 12);
            this.checkBoxEnabled.Name = "checkBoxEnabled";
            this.checkBoxEnabled.Size = new System.Drawing.Size(65, 17);
            this.checkBoxEnabled.TabIndex = 0;
            this.checkBoxEnabled.Text = "Enabled";
            this.checkBoxEnabled.UseVisualStyleBackColor = true;
            //
            // labelInfo
            //
            this.labelInfo.AutoSize = true;
            this.labelInfo.Location = new System.Drawing.Point(12, 40);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(420, 13);
            this.labelInfo.TabIndex = 1;
            this.labelInfo.Text = "Select a folder containing .mbtiles files. Each file appears as a separate map option in VRS.";
            //
            // labelFolder
            //
            this.labelFolder.AutoSize = true;
            this.labelFolder.Location = new System.Drawing.Point(12, 68);
            this.labelFolder.Name = "labelFolder";
            this.labelFolder.Size = new System.Drawing.Size(68, 13);
            this.labelFolder.TabIndex = 2;
            this.labelFolder.Text = "MBTiles Folder:";
            //
            // textBoxFolder
            //
            this.textBoxFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFolder.Location = new System.Drawing.Point(12, 86);
            this.textBoxFolder.Name = "textBoxFolder";
            this.textBoxFolder.Size = new System.Drawing.Size(510, 20);
            this.textBoxFolder.TabIndex = 3;
            this.textBoxFolder.TextChanged += new System.EventHandler(this.textBoxFolder_TextChanged);
            //
            // buttonBrowse
            //
            this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowse.Location = new System.Drawing.Point(528, 84);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 4;
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            //
            // labelFileCount
            //
            this.labelFileCount.AutoSize = true;
            this.labelFileCount.Location = new System.Drawing.Point(12, 112);
            this.labelFileCount.Name = "labelFileCount";
            this.labelFileCount.Size = new System.Drawing.Size(0, 13);
            this.labelFileCount.TabIndex = 5;
            this.labelFileCount.ForeColor = System.Drawing.Color.DarkGreen;
            //
            // checkBoxIsTms
            //
            this.checkBoxIsTms.AutoSize = true;
            this.checkBoxIsTms.Checked = true;
            this.checkBoxIsTms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIsTms.Location = new System.Drawing.Point(12, 135);
            this.checkBoxIsTms.Name = "checkBoxIsTms";
            this.checkBoxIsTms.Size = new System.Drawing.Size(280, 17);
            this.checkBoxIsTms.TabIndex = 6;
            this.checkBoxIsTms.Text = "TMS coordinate scheme (default for MBTiles spec)";
            this.checkBoxIsTms.UseVisualStyleBackColor = true;
            //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(447, 175);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 7;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(528, 175);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 8;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            //
            // OptionsView
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(615, 210);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkBoxIsTms);
            this.Controls.Add(this.labelFileCount);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textBoxFolder);
            this.Controls.Add(this.labelFolder);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.checkBoxEnabled);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tile Server MBTiles Options";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.Label labelFolder;
        private System.Windows.Forms.TextBox textBoxFolder;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelFileCount;
        private System.Windows.Forms.CheckBox checkBoxIsTms;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
