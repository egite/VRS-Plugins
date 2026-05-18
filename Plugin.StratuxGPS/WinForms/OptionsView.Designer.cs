namespace VirtualRadar.Plugin.StratuxGPS.WinForms
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
            this.labelAddress = new System.Windows.Forms.Label();
            this.textBoxAddress = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.numericPort = new System.Windows.Forms.NumericUpDown();
            this.labelPollInterval = new System.Windows.Forms.Label();
            this.numericPollInterval = new System.Windows.Forms.NumericUpDown();
            this.labelDetected = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPollInterval)).BeginInit();
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
            // labelAddress
            //
            this.labelAddress.AutoSize = true;
            this.labelAddress.Location = new System.Drawing.Point(12, 42);
            this.labelAddress.Name = "labelAddress";
            this.labelAddress.Size = new System.Drawing.Size(86, 13);
            this.labelAddress.Text = "Stratux Address:";
            //
            // textBoxAddress
            //
            this.textBoxAddress.Location = new System.Drawing.Point(112, 39);
            this.textBoxAddress.Name = "textBoxAddress";
            this.textBoxAddress.Size = new System.Drawing.Size(200, 20);
            this.textBoxAddress.TabIndex = 1;
            //
            // labelPort
            //
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(12, 68);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(29, 13);
            this.labelPort.Text = "Port:";
            //
            // numericPort
            //
            this.numericPort.Location = new System.Drawing.Point(112, 66);
            this.numericPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numericPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericPort.Name = "numericPort";
            this.numericPort.Size = new System.Drawing.Size(80, 20);
            this.numericPort.TabIndex = 2;
            this.numericPort.Value = new decimal(new int[] { 80, 0, 0, 0 });
            //
            // labelPollInterval
            //
            this.labelPollInterval.AutoSize = true;
            this.labelPollInterval.Location = new System.Drawing.Point(12, 94);
            this.labelPollInterval.Name = "labelPollInterval";
            this.labelPollInterval.Size = new System.Drawing.Size(93, 13);
            this.labelPollInterval.Text = "Poll Interval (ms):";
            //
            // numericPollInterval
            //
            this.numericPollInterval.Location = new System.Drawing.Point(112, 92);
            this.numericPollInterval.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.numericPollInterval.Minimum = new decimal(new int[] { 250, 0, 0, 0 });
            this.numericPollInterval.Name = "numericPollInterval";
            this.numericPollInterval.Size = new System.Drawing.Size(80, 20);
            this.numericPollInterval.TabIndex = 3;
            this.numericPollInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            //
            // labelDetected
            //
            this.labelDetected.AutoSize = true;
            this.labelDetected.ForeColor = System.Drawing.Color.DarkGreen;
            this.labelDetected.Location = new System.Drawing.Point(12, 122);
            this.labelDetected.Name = "labelDetected";
            this.labelDetected.Size = new System.Drawing.Size(100, 13);
            this.labelDetected.Text = "";
            //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(156, 148);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 4;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(237, 148);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            //
            // OptionsView
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(324, 183);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelDetected);
            this.Controls.Add(this.numericPollInterval);
            this.Controls.Add(this.labelPollInterval);
            this.Controls.Add(this.numericPort);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.textBoxAddress);
            this.Controls.Add(this.labelAddress);
            this.Controls.Add(this.checkBoxEnabled);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Stratux GPS Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPollInterval)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label labelAddress;
        private System.Windows.Forms.TextBox textBoxAddress;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.NumericUpDown numericPort;
        private System.Windows.Forms.Label labelPollInterval;
        private System.Windows.Forms.NumericUpDown numericPollInterval;
        private System.Windows.Forms.Label labelDetected;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
