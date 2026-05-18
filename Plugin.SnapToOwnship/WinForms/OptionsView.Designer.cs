namespace VirtualRadar.Plugin.SnapToOwnship.WinForms
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
            this.checkBoxAutoDetectIcao = new System.Windows.Forms.CheckBox();
            this.labelIcao = new System.Windows.Forms.Label();
            this.textBoxIcao = new System.Windows.Forms.TextBox();
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
            // checkBoxAutoDetectIcao
            //
            this.checkBoxAutoDetectIcao.AutoSize = true;
            this.checkBoxAutoDetectIcao.Location = new System.Drawing.Point(12, 35);
            this.checkBoxAutoDetectIcao.Name = "checkBoxAutoDetectIcao";
            this.checkBoxAutoDetectIcao.Size = new System.Drawing.Size(190, 17);
            this.checkBoxAutoDetectIcao.TabIndex = 1;
            this.checkBoxAutoDetectIcao.Text = "Auto-detect ICAO from Stratux";
            this.checkBoxAutoDetectIcao.UseVisualStyleBackColor = true;
            this.checkBoxAutoDetectIcao.CheckedChanged += new System.EventHandler(this.checkBoxAutoDetectIcao_CheckedChanged);
            //
            // labelIcao
            //
            this.labelIcao.AutoSize = true;
            this.labelIcao.Location = new System.Drawing.Point(12, 67);
            this.labelIcao.Name = "labelIcao";
            this.labelIcao.Size = new System.Drawing.Size(115, 13);
            this.labelIcao.TabIndex = 2;
            this.labelIcao.Text = "Ownship ICAO Address:";
            //
            // textBoxIcao
            //
            this.textBoxIcao.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxIcao.Location = new System.Drawing.Point(133, 64);
            this.textBoxIcao.MaxLength = 6;
            this.textBoxIcao.Name = "textBoxIcao";
            this.textBoxIcao.Size = new System.Drawing.Size(80, 20);
            this.textBoxIcao.TabIndex = 3;
            //
            // buttonOK
            //
            this.buttonOK.Location = new System.Drawing.Point(57, 103);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 4;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(138, 103);
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
            this.ClientSize = new System.Drawing.Size(230, 138);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textBoxIcao);
            this.Controls.Add(this.labelIcao);
            this.Controls.Add(this.checkBoxAutoDetectIcao);
            this.Controls.Add(this.checkBoxEnabled);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Snap to Ownship Options";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.CheckBox checkBoxAutoDetectIcao;
        private System.Windows.Forms.Label labelIcao;
        private System.Windows.Forms.TextBox textBoxIcao;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
