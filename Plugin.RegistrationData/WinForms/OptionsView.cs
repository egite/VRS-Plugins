using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.RegistrationData.WinForms
{
    public partial class OptionsView : BaseForm
    {
        public Options Options { get; set; }
        internal RegistrationDatabase Database { get; set; }

        public event EventHandler DownloadAircraftRequested;
        public event EventHandler DownloadAirmenRequested;
        public event EventHandler DownloadCcarRequested;
        public event EventHandler DownloadNtsbRequested;
        public event EventHandler DownloadSdrRequested;
        public event EventHandler DownloadCasaRequested;

        private System.Windows.Forms.Timer _PollTimer;

        public OptionsView()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode && Options != null) {
                checkBoxEnabled.Checked = Options.Enabled;
                checkBoxAutoDownloads.Checked = Options.EnableAutomaticDownloads;
                textBoxDatabaseFolder.Text = Options.DatabaseFolder ?? "";

                // On Stratux, show the USB-drive hint and push the groups below
                // it down to make room.
                if(OptionsStorage.IsStratux()) {
                    labelStratuxHint.Visible = true;
                    const int shift = 66;
                    foreach(Control c in tabSettings.Controls) {
                        if(c != labelStratuxHint && c.Top >= 62) c.Top += shift;
                    }
                }
                radioNewTab.Checked = Options.OpenInNewTab;
                radioPopup.Checked = !Options.OpenInNewTab;
                checkBoxFetchPhotos.Checked = Options.FetchAircraftPhotos;
                checkBoxDdgFallback.Checked = Options.DuckDuckGoImageFallback;
                numericAircraftInterval.Value = Math.Max(1, Math.Min(365, Options.AircraftUpdateIntervalDays));
                numericAirmenInterval.Value = Math.Max(1, Math.Min(365, Options.AirmenUpdateIntervalDays));
                numericFuzzyDistance.Value = Math.Max(0, Math.Min(5, Options.FuzzyMatchMaxDistance));
                numericCcarInterval.Value = Math.Max(1, Math.Min(365, Options.CcarUpdateIntervalDays));
                numericNtsbInterval.Value = Math.Max(1, Math.Min(365, Options.NtsbUpdateIntervalDays));
                numericSdrInterval.Value = Math.Max(1, Math.Min(365, Options.SdrUpdateIntervalDays));
                numericCasaInterval.Value = Math.Max(1, Math.Min(365, Options.CasaUpdateIntervalDays));

                textBoxAircraftUrl.Text = Options.AircraftDownloadUrl ?? "";
                textBoxAirmenUrl.Text = Options.AirmenDownloadUrl ?? "";
                textBoxCcarUrl.Text = Options.CcarDownloadUrl ?? "";
                textBoxNtsbUrl.Text = Options.NtsbDownloadUrl ?? "";
                comboWeightUnit.SelectedItem = Options.WeightUnit ?? "lbs";
                textBoxCasaUrl.Text = Options.CasaDownloadUrl ?? "";
                textBoxPinkReg.Text = Options.PinkRegistration ?? "";
                textBoxHighlightModel.Text = Options.HighlightModelIcao ?? "";
                numericPilotThreshold.Value = Math.Max(0, Math.Min(500, Options.PilotMatchThreshold));

                labelLastAircraft.Text = Options.LastAircraftDownload == DateTime.MinValue
                    ? "Never" : Options.LastAircraftDownload.ToString("g");
                labelLastAirmen.Text = Options.LastAirmenDownload == DateTime.MinValue
                    ? "Never" : Options.LastAirmenDownload.ToString("g");
                labelLastCcar.Text = Options.LastCcarDownload == DateTime.MinValue
                    ? "Never" : Options.LastCcarDownload.ToString("g");
                labelLastNtsb.Text = Options.LastNtsbDownload == DateTime.MinValue
                    ? "Never" : Options.LastNtsbDownload.ToString("g");
                labelLastSdr.Text = Options.LastSdrDownload == DateTime.MinValue
                    ? "Never" : Options.LastSdrDownload.ToString("g");
                labelLastCasa.Text = Options.LastCasaDownload == DateTime.MinValue
                    ? "Never" : Options.LastCasaDownload.ToString("g");
                labelLastNzcaa.Text = "Last import: " + (Options.LastNzcaaDownload == DateTime.MinValue
                    ? "Never" : Options.LastNzcaaDownload.ToString("g"));

                UpdateRecordCounts();

                _PollTimer = new System.Windows.Forms.Timer();
                _PollTimer.Interval = 500;
                _PollTimer.Tick += PollTimer_Tick;
                _PollTimer.Start();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if(_PollTimer != null) {
                _PollTimer.Stop();
                _PollTimer.Dispose();
                _PollTimer = null;
            }
            base.OnFormClosed(e);
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            var downloader = Plugin.Singleton?.Downloader;
            if(downloader == null) return;

            var aircraftPct = downloader.AircraftProgress;
            var aircraftPhase = downloader.AircraftPhase ?? "";
            progressBarAircraft.Value = Math.Max(0, Math.Min(100, aircraftPct));
            labelAircraftStatus.Text = aircraftPhase;

            var airmenPct = downloader.AirmenProgress;
            var airmenPhase = downloader.AirmenPhase ?? "";
            progressBarAirmen.Value = Math.Max(0, Math.Min(100, airmenPct));
            labelAirmenStatus.Text = airmenPhase;

            buttonDownloadAircraft.Enabled = !downloader.IsDownloadingAircraft;
            buttonDownloadAirmen.Enabled = !downloader.IsDownloadingAirmen;
            buttonDownloadCcar.Enabled = !downloader.IsDownloadingCcar;
            buttonDownloadNtsb.Enabled = !downloader.IsDownloadingNtsb;
            buttonDownloadSdr.Enabled = !downloader.IsDownloadingSdr;
            buttonDownloadCasa.Enabled = !downloader.IsDownloadingCasa;

            var ntsbPct = downloader.NtsbProgress;
            var ntsbPhase = downloader.NtsbPhase ?? "";
            progressBarNtsb.Value = Math.Max(0, Math.Min(100, ntsbPct));
            labelNtsbStatus.Text = ntsbPhase;

            if(ntsbPct == 100) {
                UpdateRecordCounts();
                var opts2 = OptionsStorage.Load(Plugin.Singleton);
                if(opts2.LastNtsbDownload != DateTime.MinValue) {
                    labelLastNtsb.Text = opts2.LastNtsbDownload.ToString("g");
                }
            }

            var sdrPct = downloader.SdrProgress;
            var sdrPhase = downloader.SdrPhase ?? "";
            progressBarSdr.Value = Math.Max(0, Math.Min(100, sdrPct));
            labelSdrStatus.Text = sdrPhase;

            if(sdrPct == 100) {
                UpdateRecordCounts();
                var opts3 = OptionsStorage.Load(Plugin.Singleton);
                if(opts3.LastSdrDownload != DateTime.MinValue) {
                    labelLastSdr.Text = opts3.LastSdrDownload.ToString("g");
                }
            }

            var casaPct = downloader.CasaProgress;
            var casaPhase = downloader.CasaPhase ?? "";
            progressBarCasa.Value = Math.Max(0, Math.Min(100, casaPct));
            labelCasaStatus.Text = casaPhase;

            if(casaPct == 100) {
                UpdateRecordCounts();
                var opts4 = OptionsStorage.Load(Plugin.Singleton);
                if(opts4.LastCasaDownload != DateTime.MinValue) {
                    labelLastCasa.Text = opts4.LastCasaDownload.ToString("g");
                }
            }

            if(downloader.IsDownloadingCcar) {
                progressBarCcar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
                labelCcarStatus.Text = Plugin.Singleton?.StatusDescription ?? "Downloading CCAR...";
            } else {
                if(progressBarCcar.Style == System.Windows.Forms.ProgressBarStyle.Marquee) {
                    progressBarCcar.Style = System.Windows.Forms.ProgressBarStyle.Blocks;
                    labelCcarStatus.Text = Plugin.Singleton?.StatusDescription ?? "";
                    UpdateRecordCounts();
                    // Refresh last download time
                    var opts = OptionsStorage.Load(Plugin.Singleton);
                    if(opts.LastCcarDownload != DateTime.MinValue) {
                        labelLastCcar.Text = opts.LastCcarDownload.ToString("g");
                    }
                }
            }

            if(aircraftPct == 100 || airmenPct == 100) {
                UpdateRecordCounts();
            }
        }

        private void UpdateRecordCounts()
        {
            if(Database != null) {
                try {
                    var counts = Database.GetRecordCounts();
                    long regCount, refCount, engCount, airmenCount, certCount;
                    counts.TryGetValue("aircraft_registration", out regCount);
                    counts.TryGetValue("aircraft_reference", out refCount);
                    counts.TryGetValue("engine_reference", out engCount);
                    counts.TryGetValue("airmen_basic", out airmenCount);
                    counts.TryGetValue("airmen_certificate", out certCount);

                    long ccarCount, ntsbCount, casaCount;
                    counts.TryGetValue("ccar_aircraft", out ccarCount);
                    counts.TryGetValue("ntsb_event", out ntsbCount);
                    counts.TryGetValue("casa_aircraft", out casaCount);

                    labelDbStats.Text = string.Format(
                        "FAA Aircraft: {0:N0}  |  Refs: {1:N0}  |  Engines: {2:N0}  |  Airmen: {3:N0}  |  Certs: {4:N0}  |  CCAR: {5:N0}  |  NTSB: {6:N0}  |  CASA: {7:N0}",
                        regCount, refCount, engCount, airmenCount, certCount, ccarCount, ntsbCount, casaCount);
                } catch {
                    labelDbStats.Text = "Unable to read database statistics.";
                }
            } else {
                labelDbStats.Text = "Database not initialized.";
            }
        }

        private void buttonBrowseFolder_Click(object sender, EventArgs e)
        {
            using(var dialog = new FolderBrowserDialog()) {
                dialog.Description = "Select FAA database folder";
                if(Directory.Exists(textBoxDatabaseFolder.Text)) {
                    dialog.SelectedPath = textBoxDatabaseFolder.Text;
                }
                if(dialog.ShowDialog() == DialogResult.OK) {
                    textBoxDatabaseFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void buttonDownloadAircraft_Click(object sender, EventArgs e)
        {
            buttonDownloadAircraft.Enabled = false;
            DownloadAircraftRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonDownloadAirmen_Click(object sender, EventArgs e)
        {
            buttonDownloadAirmen.Enabled = false;
            DownloadAirmenRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonDownloadCcar_Click(object sender, EventArgs e)
        {
            buttonDownloadCcar.Enabled = false;
            DownloadCcarRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonDownloadNtsb_Click(object sender, EventArgs e)
        {
            buttonDownloadNtsb.Enabled = false;
            DownloadNtsbRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonDownloadSdr_Click(object sender, EventArgs e)
        {
            buttonDownloadSdr.Enabled = false;
            DownloadSdrRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonNzcaaPage_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.aviation.govt.nz/aircraft/aircraft-registration/aircraft-register-search/");
        }

        private void buttonNzcaaImport_Click(object sender, EventArgs e)
        {
            buttonNzcaaImport.Enabled = false;
            DownloadCasaRequested?.Invoke(this, EventArgs.Empty); // reuse event, downloader handles NZ separately
            ThreadPool.QueueUserWorkItem(_ => {
                try { Plugin.Singleton?.Downloader?.ImportNzcaaDatabase(); } catch { }
            });
        }

        private void buttonDownloadCasa_Click(object sender, EventArgs e)
        {
            buttonDownloadCasa.Enabled = false;
            DownloadCasaRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.EnableAutomaticDownloads = checkBoxAutoDownloads.Checked;
            Options.DatabaseFolder = textBoxDatabaseFolder.Text.Trim();
            Options.OpenInNewTab = radioNewTab.Checked;
            Options.FetchAircraftPhotos = checkBoxFetchPhotos.Checked;
            Options.DuckDuckGoImageFallback = checkBoxDdgFallback.Checked;
            Options.AircraftUpdateIntervalDays = (int)numericAircraftInterval.Value;
            Options.AirmenUpdateIntervalDays = (int)numericAirmenInterval.Value;
            Options.CcarUpdateIntervalDays = (int)numericCcarInterval.Value;
            Options.NtsbUpdateIntervalDays = (int)numericNtsbInterval.Value;
            Options.SdrUpdateIntervalDays = (int)numericSdrInterval.Value;
            Options.CasaUpdateIntervalDays = (int)numericCasaInterval.Value;
            Options.FuzzyMatchMaxDistance = (int)numericFuzzyDistance.Value;
            Options.AircraftDownloadUrl = textBoxAircraftUrl.Text.Trim();
            Options.AirmenDownloadUrl = textBoxAirmenUrl.Text.Trim();
            Options.CcarDownloadUrl = textBoxCcarUrl.Text.Trim();
            Options.NtsbDownloadUrl = textBoxNtsbUrl.Text.Trim();
            Options.WeightUnit = (comboWeightUnit.SelectedItem ?? "lbs").ToString();
            Options.CasaDownloadUrl = textBoxCasaUrl.Text.Trim();
            Options.PinkRegistration = textBoxPinkReg.Text.Trim().ToUpperInvariant();
            Options.HighlightModelIcao = textBoxHighlightModel.Text.Trim().ToUpperInvariant();
            Options.PilotMatchThreshold = (int)numericPilotThreshold.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
