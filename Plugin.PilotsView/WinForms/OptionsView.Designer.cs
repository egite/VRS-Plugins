namespace VirtualRadar.Plugin.PilotsView.WinForms
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
            this.labelRefresh = new System.Windows.Forms.Label();
            this.numericRefreshInterval = new System.Windows.Forms.NumericUpDown();
            this.labelRefreshSuffix = new System.Windows.Forms.Label();
            this.labelTilt = new System.Windows.Forms.Label();
            this.numericCameraTilt = new System.Windows.Forms.NumericUpDown();
            this.labelTiltSuffix = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericRefreshInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCameraTilt)).BeginInit();
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
            // labelRefresh
            //
            this.labelRefresh.AutoSize = true;
            this.labelRefresh.Location = new System.Drawing.Point(12, 44);
            this.labelRefresh.Name = "labelRefresh";
            this.labelRefresh.Size = new System.Drawing.Size(87, 13);
            this.labelRefresh.TabIndex = 1;
            this.labelRefresh.Text = "Refresh Interval:";
            //
            // numericRefreshInterval
            //
            this.numericRefreshInterval.Location = new System.Drawing.Point(112, 42);
            this.numericRefreshInterval.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numericRefreshInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericRefreshInterval.Name = "numericRefreshInterval";
            this.numericRefreshInterval.Size = new System.Drawing.Size(50, 20);
            this.numericRefreshInterval.TabIndex = 2;
            this.numericRefreshInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // labelRefreshSuffix
            //
            this.labelRefreshSuffix.AutoSize = true;
            this.labelRefreshSuffix.Location = new System.Drawing.Point(168, 44);
            this.labelRefreshSuffix.Name = "labelRefreshSuffix";
            this.labelRefreshSuffix.Size = new System.Drawing.Size(47, 13);
            this.labelRefreshSuffix.TabIndex = 3;
            this.labelRefreshSuffix.Text = "seconds";
            //
            // labelTilt
            //
            this.labelTilt.AutoSize = true;
            this.labelTilt.Location = new System.Drawing.Point(12, 74);
            this.labelTilt.Name = "labelTilt";
            this.labelTilt.Size = new System.Drawing.Size(68, 13);
            this.labelTilt.TabIndex = 4;
            this.labelTilt.Text = "Camera Tilt:";
            //
            // numericCameraTilt
            //
            this.numericCameraTilt.Location = new System.Drawing.Point(112, 72);
            this.numericCameraTilt.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            this.numericCameraTilt.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numericCameraTilt.Name = "numericCameraTilt";
            this.numericCameraTilt.Size = new System.Drawing.Size(50, 20);
            this.numericCameraTilt.TabIndex = 5;
            this.numericCameraTilt.Value = new decimal(new int[] { 80, 0, 0, 0 });
            //
            // labelTiltSuffix
            //
            this.labelTiltSuffix.AutoSize = true;
            this.labelTiltSuffix.Location = new System.Drawing.Point(168, 74);
            this.labelTiltSuffix.Name = "labelTiltSuffix";
            this.labelTiltSuffix.Size = new System.Drawing.Size(130, 13);
            this.labelTiltSuffix.TabIndex = 6;
            this.labelTiltSuffix.Text = "degrees (0=down, 90=horizon)";
            //
            // buttonOK
            //
            this.buttonOK.Location = new System.Drawing.Point(80, 110);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 7;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(161, 110);
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
            this.ClientSize = new System.Drawing.Size(310, 145);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelTiltSuffix);
            this.Controls.Add(this.numericCameraTilt);
            this.Controls.Add(this.labelTilt);
            this.Controls.Add(this.labelRefreshSuffix);
            this.Controls.Add(this.numericRefreshInterval);
            this.Controls.Add(this.labelRefresh);
            this.Controls.Add(this.checkBoxEnabled);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Pilot's View Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericRefreshInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCameraTilt)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label labelRefresh;
        private System.Windows.Forms.NumericUpDown numericRefreshInterval;
        private System.Windows.Forms.Label labelRefreshSuffix;
        private System.Windows.Forms.Label labelTilt;
        private System.Windows.Forms.NumericUpDown numericCameraTilt;
        private System.Windows.Forms.Label labelTiltSuffix;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
