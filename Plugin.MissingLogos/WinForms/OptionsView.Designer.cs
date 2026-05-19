namespace VirtualRadar.Plugin.MissingLogos.WinForms
{
    partial class OptionsView
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
            if(disposing && (components != null)) {
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
            this.checkBoxEnabled = new System.Windows.Forms.CheckBox();
            this.labelLogFile = new System.Windows.Forms.Label();
            this.textBoxLogFileName = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.checkBoxTrackMissingModels = new System.Windows.Forms.CheckBox();
            this.labelModelLogFile = new System.Windows.Forms.Label();
            this.textBoxModelLogFileName = new System.Windows.Forms.TextBox();
            this.buttonBrowseModelLog = new System.Windows.Forms.Button();
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
            // labelLogFile
            //
            this.labelLogFile.AutoSize = true;
            this.labelLogFile.Location = new System.Drawing.Point(12, 42);
            this.labelLogFile.Name = "labelLogFile";
            this.labelLogFile.Size = new System.Drawing.Size(47, 13);
            this.labelLogFile.TabIndex = 1;
            this.labelLogFile.Text = "Log file:";
            //
            // textBoxLogFileName
            //
            this.textBoxLogFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLogFileName.Location = new System.Drawing.Point(65, 39);
            this.textBoxLogFileName.Name = "textBoxLogFileName";
            this.textBoxLogFileName.Size = new System.Drawing.Size(370, 20);
            this.textBoxLogFileName.TabIndex = 2;
            //
            // buttonBrowse
            //
            this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowse.Location = new System.Drawing.Point(441, 37);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 3;
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            //
            // checkBoxTrackMissingModels
            //
            this.checkBoxTrackMissingModels.AutoSize = true;
            this.checkBoxTrackMissingModels.Location = new System.Drawing.Point(12, 72);
            this.checkBoxTrackMissingModels.Name = "checkBoxTrackMissingModels";
            this.checkBoxTrackMissingModels.Size = new System.Drawing.Size(133, 17);
            this.checkBoxTrackMissingModels.TabIndex = 4;
            this.checkBoxTrackMissingModels.Text = "Track missing models";
            this.checkBoxTrackMissingModels.UseVisualStyleBackColor = true;
            //
            // labelModelLogFile
            //
            this.labelModelLogFile.AutoSize = true;
            this.labelModelLogFile.Location = new System.Drawing.Point(12, 100);
            this.labelModelLogFile.Name = "labelModelLogFile";
            this.labelModelLogFile.Size = new System.Drawing.Size(47, 13);
            this.labelModelLogFile.TabIndex = 5;
            this.labelModelLogFile.Text = "Log file:";
            //
            // textBoxModelLogFileName
            //
            this.textBoxModelLogFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxModelLogFileName.Location = new System.Drawing.Point(65, 97);
            this.textBoxModelLogFileName.Name = "textBoxModelLogFileName";
            this.textBoxModelLogFileName.Size = new System.Drawing.Size(370, 20);
            this.textBoxModelLogFileName.TabIndex = 6;
            //
            // buttonBrowseModelLog
            //
            this.buttonBrowseModelLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseModelLog.Location = new System.Drawing.Point(441, 95);
            this.buttonBrowseModelLog.Name = "buttonBrowseModelLog";
            this.buttonBrowseModelLog.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseModelLog.TabIndex = 7;
            this.buttonBrowseModelLog.Text = "Browse...";
            this.buttonBrowseModelLog.UseVisualStyleBackColor = true;
            this.buttonBrowseModelLog.Click += new System.EventHandler(this.buttonBrowseModelLog_Click);
            //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(360, 135);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 8;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(441, 135);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            //
            // OptionsView
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(528, 170);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonBrowseModelLog);
            this.Controls.Add(this.textBoxModelLogFileName);
            this.Controls.Add(this.labelModelLogFile);
            this.Controls.Add(this.checkBoxTrackMissingModels);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textBoxLogFileName);
            this.Controls.Add(this.labelLogFile);
            this.Controls.Add(this.checkBoxEnabled);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Missing Logos Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label labelLogFile;
        private System.Windows.Forms.TextBox textBoxLogFileName;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.CheckBox checkBoxTrackMissingModels;
        private System.Windows.Forms.Label labelModelLogFile;
        private System.Windows.Forms.TextBox textBoxModelLogFileName;
        private System.Windows.Forms.Button buttonBrowseModelLog;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
