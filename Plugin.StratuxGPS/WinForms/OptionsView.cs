using System;
using System.Globalization;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.StratuxGPS.WinForms
{
    /// <summary>
    /// The WinForms options dialog for the Stratux GPS plugin.
    /// </summary>
    public partial class OptionsView : BaseForm
    {
        private Timer _PositionRefreshTimer;

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

                RefreshPosition();
                _PositionRefreshTimer = new Timer { Interval = Math.Max(250, Options.PollIntervalMilliseconds) };
                _PositionRefreshTimer.Tick += (s, a) => RefreshPosition();
                _PositionRefreshTimer.Start();
            }
        }

        /// <summary>
        /// See base docs.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if(_PositionRefreshTimer != null) {
                _PositionRefreshTimer.Stop();
                _PositionRefreshTimer.Dispose();
                _PositionRefreshTimer = null;
            }
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Pulls the latest position snapshot from the plugin singleton and updates the
        /// Detected Position panel.
        /// </summary>
        private void RefreshPosition()
        {
            var plugin = Plugin.Singleton;
            var position = plugin != null ? plugin.GetCurrentPosition() : null;

            if(position == null || !position.HasPosition) {
                labelPositionStatus.Text = position == null ? "Plugin not running" : "Waiting for GPS fix…";
                labelPositionStatus.Visible = true;
                labelLatValue.Text = "—";
                labelLngValue.Text = "—";
                labelAltValue.Text = "—";
                labelSpdValue.Text = "—";
            } else {
                labelPositionStatus.Text = $"GPS fix ({position.FixQuality}), age {position.AgeSeconds:F1}s";
                labelPositionStatus.Visible = true;
                labelLatValue.Text = position.Latitude.ToString("F6", CultureInfo.InvariantCulture) + "°";
                labelLngValue.Text = position.Longitude.ToString("F6", CultureInfo.InvariantCulture) + "°";
                labelAltValue.Text = position.AltitudeFeet.ToString("F0", CultureInfo.InvariantCulture) + " ft";
                labelSpdValue.Text = position.GroundSpeedKnots.ToString("F1", CultureInfo.InvariantCulture) + " kts";
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
