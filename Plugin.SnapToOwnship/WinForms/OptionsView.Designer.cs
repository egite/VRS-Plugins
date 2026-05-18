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
            this.labelStratuxNote = new System.Windows.Forms.Label();
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
            // labelStratuxNote
            //
            this.labelStratuxNote.Location = new System.Drawing.Point(12, 95);
            this.labelStratuxNote.Name = "labelStratuxNote";
            this.labelStratuxNote.Size = new System.Drawing.Size(436, 60);
            this.labelStratuxNote.TabIndex = 4;
            this.labelStratuxNote.ForeColor = System.Drawing.Color.FromArgb(120, 60, 0);
            this.labelStratuxNote.Text = "Note: When VRS runs on a Stratux device, a manually-entered ICAO here will not be saved across reboots / power cycles. To persist, set OwnshipModeS on the Stratux device and enable auto-detect above.";
            //
            // buttonOK
            //
            this.buttonOK.Location = new System.Drawing.Point(172, 168);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(253, 168);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            //
            // OptionsView
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(460, 203);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelStratuxNote);
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
        private System.Windows.Forms.Label labelStratuxNote;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
