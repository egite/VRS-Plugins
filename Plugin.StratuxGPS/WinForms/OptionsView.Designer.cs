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
            this.groupBoxPosition = new System.Windows.Forms.GroupBox();
            this.labelPositionStatus = new System.Windows.Forms.Label();
            this.labelLatCaption = new System.Windows.Forms.Label();
            this.labelLngCaption = new System.Windows.Forms.Label();
            this.labelAltCaption = new System.Windows.Forms.Label();
            this.labelSpdCaption = new System.Windows.Forms.Label();
            this.labelLatValue = new System.Windows.Forms.Label();
            this.labelLngValue = new System.Windows.Forms.Label();
            this.labelAltValue = new System.Windows.Forms.Label();
            this.labelSpdValue = new System.Windows.Forms.Label();
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
            // groupBoxPosition
            //
            this.groupBoxPosition.Location = new System.Drawing.Point(12, 145);
            this.groupBoxPosition.Name = "groupBoxPosition";
            this.groupBoxPosition.Size = new System.Drawing.Size(300, 115);
            this.groupBoxPosition.TabIndex = 6;
            this.groupBoxPosition.TabStop = false;
            this.groupBoxPosition.Text = "Detected Position";
            this.groupBoxPosition.Controls.Add(this.labelPositionStatus);
            this.groupBoxPosition.Controls.Add(this.labelLatCaption);
            this.groupBoxPosition.Controls.Add(this.labelLngCaption);
            this.groupBoxPosition.Controls.Add(this.labelAltCaption);
            this.groupBoxPosition.Controls.Add(this.labelSpdCaption);
            this.groupBoxPosition.Controls.Add(this.labelLatValue);
            this.groupBoxPosition.Controls.Add(this.labelLngValue);
            this.groupBoxPosition.Controls.Add(this.labelAltValue);
            this.groupBoxPosition.Controls.Add(this.labelSpdValue);
            //
            // labelPositionStatus
            //
            this.labelPositionStatus.AutoSize = true;
            this.labelPositionStatus.ForeColor = System.Drawing.Color.Gray;
            this.labelPositionStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
            this.labelPositionStatus.Location = new System.Drawing.Point(10, 18);
            this.labelPositionStatus.Name = "labelPositionStatus";
            this.labelPositionStatus.Size = new System.Drawing.Size(60, 13);
            this.labelPositionStatus.Text = "Waiting for GPS fix…";
            //
            // labelLatCaption
            //
            this.labelLatCaption.AutoSize = true;
            this.labelLatCaption.Location = new System.Drawing.Point(10, 38);
            this.labelLatCaption.Name = "labelLatCaption";
            this.labelLatCaption.Size = new System.Drawing.Size(55, 13);
            this.labelLatCaption.Text = "Latitude:";
            //
            // labelLngCaption
            //
            this.labelLngCaption.AutoSize = true;
            this.labelLngCaption.Location = new System.Drawing.Point(10, 56);
            this.labelLngCaption.Name = "labelLngCaption";
            this.labelLngCaption.Size = new System.Drawing.Size(60, 13);
            this.labelLngCaption.Text = "Longitude:";
            //
            // labelAltCaption
            //
            this.labelAltCaption.AutoSize = true;
            this.labelAltCaption.Location = new System.Drawing.Point(10, 74);
            this.labelAltCaption.Name = "labelAltCaption";
            this.labelAltCaption.Size = new System.Drawing.Size(55, 13);
            this.labelAltCaption.Text = "Altitude:";
            //
            // labelSpdCaption
            //
            this.labelSpdCaption.AutoSize = true;
            this.labelSpdCaption.Location = new System.Drawing.Point(10, 92);
            this.labelSpdCaption.Name = "labelSpdCaption";
            this.labelSpdCaption.Size = new System.Drawing.Size(45, 13);
            this.labelSpdCaption.Text = "Speed:";
            //
            // labelLatValue
            //
            this.labelLatValue.AutoSize = true;
            this.labelLatValue.Location = new System.Drawing.Point(90, 38);
            this.labelLatValue.Name = "labelLatValue";
            this.labelLatValue.Size = new System.Drawing.Size(15, 13);
            this.labelLatValue.Text = "—";
            //
            // labelLngValue
            //
            this.labelLngValue.AutoSize = true;
            this.labelLngValue.Location = new System.Drawing.Point(90, 56);
            this.labelLngValue.Name = "labelLngValue";
            this.labelLngValue.Size = new System.Drawing.Size(15, 13);
            this.labelLngValue.Text = "—";
            //
            // labelAltValue
            //
            this.labelAltValue.AutoSize = true;
            this.labelAltValue.Location = new System.Drawing.Point(90, 74);
            this.labelAltValue.Name = "labelAltValue";
            this.labelAltValue.Size = new System.Drawing.Size(15, 13);
            this.labelAltValue.Text = "—";
            //
            // labelSpdValue
            //
            this.labelSpdValue.AutoSize = true;
            this.labelSpdValue.Location = new System.Drawing.Point(90, 92);
            this.labelSpdValue.Name = "labelSpdValue";
            this.labelSpdValue.Size = new System.Drawing.Size(15, 13);
            this.labelSpdValue.Text = "—";
            //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(156, 270);
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
            this.buttonCancel.Location = new System.Drawing.Point(237, 270);
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
            this.ClientSize = new System.Drawing.Size(324, 305);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.groupBoxPosition);
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
        private System.Windows.Forms.GroupBox groupBoxPosition;
        private System.Windows.Forms.Label labelPositionStatus;
        private System.Windows.Forms.Label labelLatCaption;
        private System.Windows.Forms.Label labelLngCaption;
        private System.Windows.Forms.Label labelAltCaption;
        private System.Windows.Forms.Label labelSpdCaption;
        private System.Windows.Forms.Label labelLatValue;
        private System.Windows.Forms.Label labelLngValue;
        private System.Windows.Forms.Label labelAltValue;
        private System.Windows.Forms.Label labelSpdValue;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
