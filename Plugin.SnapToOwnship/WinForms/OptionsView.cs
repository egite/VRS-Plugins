using System;
using System.Drawing;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.SnapToOwnship.WinForms
{
    public partial class OptionsView : BaseForm
    {
        private Timer _DetectTimer;
        private string _ManualIcaoBuffer = "";

        public Options Options { get; set; }

        public OptionsView()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode) {
                checkBoxEnabled.Checked = Options.Enabled;
                checkBoxAutoDetectIcao.Checked = Options.AutoDetectIcao;
                _ManualIcaoBuffer = (Options.OwnshipIcao ?? "");
                ApplyIcaoMode();

                _DetectTimer = new Timer { Interval = 2000 };
                _DetectTimer.Tick += (s, a) => { if(checkBoxAutoDetectIcao.Checked) RefreshDetectedIcao(); };
                _DetectTimer.Start();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if(_DetectTimer != null) {
                _DetectTimer.Stop();
                _DetectTimer.Dispose();
                _DetectTimer = null;
            }
            base.OnFormClosed(e);
        }

        private void checkBoxAutoDetectIcao_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBoxAutoDetectIcao.Checked) {
                // Stash whatever the user typed so we can restore it if they uncheck.
                _ManualIcaoBuffer = textBoxIcao.Text;
            }
            ApplyIcaoMode();
        }

        private void ApplyIcaoMode()
        {
            if(checkBoxAutoDetectIcao.Checked) {
                textBoxIcao.Enabled = false;
                textBoxIcao.ForeColor = SystemColors.GrayText;
                RefreshDetectedIcao();
            } else {
                textBoxIcao.Enabled = true;
                textBoxIcao.ForeColor = SystemColors.WindowText;
                textBoxIcao.Text = _ManualIcaoBuffer ?? "";
            }
        }

        private void RefreshDetectedIcao()
        {
            var detected = StratuxBridge.TryGetOwnshipIcao();
            textBoxIcao.Text = string.IsNullOrEmpty(detected) ? "" : detected;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.AutoDetectIcao = checkBoxAutoDetectIcao.Checked;
            // When auto-detect is on, preserve whatever the user previously typed (don't overwrite
            // their manual setting with a transient auto-detected value).
            var icao = checkBoxAutoDetectIcao.Checked
                ? (_ManualIcaoBuffer ?? "").Trim().ToUpperInvariant()
                : textBoxIcao.Text.Trim().ToUpperInvariant();
            Options.OwnshipIcao = icao;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
