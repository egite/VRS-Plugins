using System;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.Stratux.WinForms
{
    /// <summary>
    /// The WinForms options dialog for the Stratux plugin.
    /// </summary>
    public partial class OptionsView : BaseForm
    {
        /// <summary>
        /// Gets or sets the options being edited.
        /// </summary>
        public Options Options { get; set; }

        /// <summary>
        /// Gets or sets the auto-detected Stratux address, if any.
        /// </summary>
        public string DetectedAddress { get; set; }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        public OptionsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// See base docs.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode) {
                checkBoxEnabled.Checked = Options.Enabled;
                textBoxAddress.Text = Options.StratuxAddress;
                numericPort.Value = Options.StratuxPort;
                numericPollInterval.Value = Options.PollIntervalMilliseconds;

                if(!string.IsNullOrEmpty(DetectedAddress)) {
                    labelDetected.Text = $"Detected Stratux network: {DetectedAddress}";
                    if(string.IsNullOrWhiteSpace(Options.StratuxAddress) ||
                       Options.StratuxAddress == "192.168.10.1") {
                        textBoxAddress.Text = DetectedAddress;
                    }
                } else {
                    labelDetected.Text = "Stratux network not detected";
                }
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.StratuxAddress = textBoxAddress.Text.Trim();
            Options.StratuxPort = (int)numericPort.Value;
            Options.PollIntervalMilliseconds = (int)numericPollInterval.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
