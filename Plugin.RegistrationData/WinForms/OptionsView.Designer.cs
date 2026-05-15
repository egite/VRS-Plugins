namespace VirtualRadar.Plugin.RegistrationData.WinForms
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.tabDownloads = new System.Windows.Forms.TabPage();

            // Settings tab controls
            this.checkBoxEnabled = new System.Windows.Forms.CheckBox();
            this.labelFolderLabel = new System.Windows.Forms.Label();
            this.textBoxDatabaseFolder = new System.Windows.Forms.TextBox();
            this.buttonBrowseFolder = new System.Windows.Forms.Button();
            this.groupBoxDisplay = new System.Windows.Forms.GroupBox();
            this.radioNewTab = new System.Windows.Forms.RadioButton();
            this.radioPopup = new System.Windows.Forms.RadioButton();
            this.checkBoxFetchPhotos = new System.Windows.Forms.CheckBox();
            this.checkBoxDdgFallback = new System.Windows.Forms.CheckBox();
            this.groupBoxPilot = new System.Windows.Forms.GroupBox();
            this.labelFuzzyLabel = new System.Windows.Forms.Label();
            this.numericFuzzyDistance = new System.Windows.Forms.NumericUpDown();
            this.labelPilotThreshold = new System.Windows.Forms.Label();
            this.numericPilotThreshold = new System.Windows.Forms.NumericUpDown();
            this.groupBoxColoring = new System.Windows.Forms.GroupBox();
            this.labelPinkReg = new System.Windows.Forms.Label();
            this.textBoxPinkReg = new System.Windows.Forms.TextBox();
            this.labelHighlightModel = new System.Windows.Forms.Label();
            this.textBoxHighlightModel = new System.Windows.Forms.TextBox();

            // Downloads tab controls
            this.groupBoxAircraftDownload = new System.Windows.Forms.GroupBox();
            this.buttonDownloadAircraft = new System.Windows.Forms.Button();
            this.progressBarAircraft = new System.Windows.Forms.ProgressBar();
            this.labelAircraftStatus = new System.Windows.Forms.Label();
            this.labelLastAircraftLabel = new System.Windows.Forms.Label();
            this.labelLastAircraft = new System.Windows.Forms.Label();
            this.labelAircraftInterval = new System.Windows.Forms.Label();
            this.numericAircraftInterval = new System.Windows.Forms.NumericUpDown();
            this.groupBoxAirmenDownload = new System.Windows.Forms.GroupBox();
            this.buttonDownloadAirmen = new System.Windows.Forms.Button();
            this.progressBarAirmen = new System.Windows.Forms.ProgressBar();
            this.labelAirmenStatus = new System.Windows.Forms.Label();
            this.labelLastAirmenLabel = new System.Windows.Forms.Label();
            this.labelLastAirmen = new System.Windows.Forms.Label();
            this.labelAirmenInterval = new System.Windows.Forms.Label();
            this.numericAirmenInterval = new System.Windows.Forms.NumericUpDown();
            this.groupBoxCcarDownload = new System.Windows.Forms.GroupBox();
            this.buttonDownloadCcar = new System.Windows.Forms.Button();
            this.progressBarCcar = new System.Windows.Forms.ProgressBar();
            this.labelCcarStatus = new System.Windows.Forms.Label();
            this.labelLastCcarLabel = new System.Windows.Forms.Label();
            this.labelLastCcar = new System.Windows.Forms.Label();
            this.labelCcarIntervalLabel = new System.Windows.Forms.Label();
            this.numericCcarInterval = new System.Windows.Forms.NumericUpDown();
            this.groupBoxNtsbDownload = new System.Windows.Forms.GroupBox();
            this.buttonDownloadNtsb = new System.Windows.Forms.Button();
            this.progressBarNtsb = new System.Windows.Forms.ProgressBar();
            this.labelNtsbStatus = new System.Windows.Forms.Label();
            this.labelLastNtsbLabel = new System.Windows.Forms.Label();
            this.labelLastNtsb = new System.Windows.Forms.Label();
            this.labelNtsbIntervalLabel = new System.Windows.Forms.Label();
            this.numericNtsbInterval = new System.Windows.Forms.NumericUpDown();
            this.groupBoxSdrDownload = new System.Windows.Forms.GroupBox();
            this.buttonDownloadSdr = new System.Windows.Forms.Button();
            this.progressBarSdr = new System.Windows.Forms.ProgressBar();
            this.labelSdrStatus = new System.Windows.Forms.Label();
            this.labelLastSdrLabel = new System.Windows.Forms.Label();
            this.labelLastSdr = new System.Windows.Forms.Label();
            this.labelSdrIntervalLabel = new System.Windows.Forms.Label();
            this.numericSdrInterval = new System.Windows.Forms.NumericUpDown();
            this.groupBoxCasaDownload = new System.Windows.Forms.GroupBox();
            this.buttonDownloadCasa = new System.Windows.Forms.Button();
            this.progressBarCasa = new System.Windows.Forms.ProgressBar();
            this.labelCasaStatus = new System.Windows.Forms.Label();
            this.labelLastCasaLabel = new System.Windows.Forms.Label();
            this.labelLastCasa = new System.Windows.Forms.Label();
            this.labelCasaIntervalLabel = new System.Windows.Forms.Label();
            this.numericCasaInterval = new System.Windows.Forms.NumericUpDown();
            this.labelCasaUrl = new System.Windows.Forms.Label();
            this.textBoxCasaUrl = new System.Windows.Forms.TextBox();
            this.groupBoxNzcaa = new System.Windows.Forms.GroupBox();
            this.buttonNzcaaPage = new System.Windows.Forms.Button();
            this.buttonNzcaaImport = new System.Windows.Forms.Button();
            this.labelLastNzcaa = new System.Windows.Forms.Label();
            this.groupBoxUrls = new System.Windows.Forms.GroupBox();
            this.labelAircraftUrl = new System.Windows.Forms.Label();
            this.textBoxAircraftUrl = new System.Windows.Forms.TextBox();
            this.labelAirmenUrl = new System.Windows.Forms.Label();
            this.textBoxAirmenUrl = new System.Windows.Forms.TextBox();
            this.labelCcarUrl = new System.Windows.Forms.Label();
            this.textBoxCcarUrl = new System.Windows.Forms.TextBox();
            this.labelNtsbUrl = new System.Windows.Forms.Label();
            this.textBoxNtsbUrl = new System.Windows.Forms.TextBox();
            this.labelDbStats = new System.Windows.Forms.Label();

            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();

            this.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.tabDownloads.SuspendLayout();

            // === TAB CONTROL ===
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Size = new System.Drawing.Size(571, 505);
            this.tabControl.TabPages.Add(this.tabSettings);
            this.tabControl.TabPages.Add(this.tabDownloads);

            // === SETTINGS TAB ===
            this.tabSettings.Text = "Settings";
            this.tabSettings.AutoScroll = true;
            this.tabSettings.Padding = new System.Windows.Forms.Padding(6);

            // Enabled
            this.checkBoxEnabled.AutoSize = true;
            this.checkBoxEnabled.Location = new System.Drawing.Point(12, 12);
            this.checkBoxEnabled.Text = "Enabled";
            this.tabSettings.Controls.Add(this.checkBoxEnabled);

            // Database Folder
            this.labelFolderLabel.AutoSize = true;
            this.labelFolderLabel.Location = new System.Drawing.Point(12, 38);
            this.labelFolderLabel.Text = "Database folder:";
            this.textBoxDatabaseFolder.Location = new System.Drawing.Point(110, 35);
            this.textBoxDatabaseFolder.Size = new System.Drawing.Size(360, 20);
            this.buttonBrowseFolder.Location = new System.Drawing.Point(476, 33);
            this.buttonBrowseFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseFolder.Text = "Browse...";
            this.buttonBrowseFolder.Click += new System.EventHandler(this.buttonBrowseFolder_Click);
            this.tabSettings.Controls.Add(this.labelFolderLabel);
            this.tabSettings.Controls.Add(this.textBoxDatabaseFolder);
            this.tabSettings.Controls.Add(this.buttonBrowseFolder);

            // Display group
            this.groupBoxDisplay.Location = new System.Drawing.Point(12, 62);
            this.groupBoxDisplay.Size = new System.Drawing.Size(535, 48);
            this.groupBoxDisplay.Text = "Display";
            this.radioNewTab.AutoSize = true;
            this.radioNewTab.Location = new System.Drawing.Point(10, 19);
            this.radioNewTab.Text = "New tab";
            this.radioPopup.AutoSize = true;
            this.radioPopup.Location = new System.Drawing.Point(90, 19);
            this.radioPopup.Text = "Popup";
            this.checkBoxFetchPhotos.AutoSize = true;
            this.checkBoxFetchPhotos.Location = new System.Drawing.Point(170, 19);
            this.checkBoxFetchPhotos.Text = "Fetch photos";
            this.checkBoxDdgFallback.AutoSize = true;
            this.checkBoxDdgFallback.Location = new System.Drawing.Point(290, 19);
            this.checkBoxDdgFallback.Text = "DuckDuckGo fallback";
            this.labelWeightUnit = new System.Windows.Forms.Label();
            this.labelWeightUnit.AutoSize = true;
            this.labelWeightUnit.Location = new System.Drawing.Point(410, 22);
            this.labelWeightUnit.Text = "Weight:";
            this.comboWeightUnit = new System.Windows.Forms.ComboBox();
            this.comboWeightUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboWeightUnit.Location = new System.Drawing.Point(455, 19);
            this.comboWeightUnit.Size = new System.Drawing.Size(65, 21);
            this.comboWeightUnit.Items.AddRange(new object[] { "lbs", "kg" });
            this.groupBoxDisplay.Controls.Add(this.radioNewTab);
            this.groupBoxDisplay.Controls.Add(this.radioPopup);
            this.groupBoxDisplay.Controls.Add(this.checkBoxFetchPhotos);
            this.groupBoxDisplay.Controls.Add(this.checkBoxDdgFallback);
            this.groupBoxDisplay.Controls.Add(this.labelWeightUnit);
            this.groupBoxDisplay.Controls.Add(this.comboWeightUnit);
            this.tabSettings.Controls.Add(this.groupBoxDisplay);

            // Pilot matching group
            this.groupBoxPilot = new System.Windows.Forms.GroupBox();
            this.groupBoxPilot.Location = new System.Drawing.Point(12, 116);
            this.groupBoxPilot.Size = new System.Drawing.Size(535, 50);
            this.groupBoxPilot.Text = "Pilot Matching";
            this.labelFuzzyLabel.AutoSize = true;
            this.labelFuzzyLabel.Location = new System.Drawing.Point(10, 22);
            this.labelFuzzyLabel.Text = "Fuzzy distance:";
            this.numericFuzzyDistance.Location = new System.Drawing.Point(100, 20);
            this.numericFuzzyDistance.Size = new System.Drawing.Size(50, 20);
            this.numericFuzzyDistance.Minimum = 0;
            this.numericFuzzyDistance.Maximum = 5;
            this.labelPilotThreshold.AutoSize = true;
            this.labelPilotThreshold.Location = new System.Drawing.Point(170, 22);
            this.labelPilotThreshold.Text = "Green min pts:";
            this.numericPilotThreshold.Location = new System.Drawing.Point(260, 20);
            this.numericPilotThreshold.Size = new System.Drawing.Size(60, 20);
            this.numericPilotThreshold.Minimum = 0;
            this.numericPilotThreshold.Maximum = 500;
            this.numericPilotThreshold.Value = 120;
            this.groupBoxPilot.Controls.Add(this.labelFuzzyLabel);
            this.groupBoxPilot.Controls.Add(this.numericFuzzyDistance);
            this.groupBoxPilot.Controls.Add(this.labelPilotThreshold);
            this.groupBoxPilot.Controls.Add(this.numericPilotThreshold);
            this.tabSettings.Controls.Add(this.groupBoxPilot);

            // Icon coloring group
            this.groupBoxColoring.Location = new System.Drawing.Point(12, 172);
            this.groupBoxColoring.Size = new System.Drawing.Size(535, 75);
            this.groupBoxColoring.Text = "Icon Coloring";
            this.labelPinkReg.AutoSize = true;
            this.labelPinkReg.Location = new System.Drawing.Point(10, 22);
            this.labelPinkReg.Text = "Ownship (comma sep.):";
            this.textBoxPinkReg.Location = new System.Drawing.Point(155, 19);
            this.textBoxPinkReg.Size = new System.Drawing.Size(365, 20);
            this.textBoxPinkReg.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.labelHighlightModel.AutoSize = true;
            this.labelHighlightModel.Location = new System.Drawing.Point(10, 48);
            this.labelHighlightModel.Text = "Highlight models (comma sep.):";
            this.textBoxHighlightModel.Location = new System.Drawing.Point(200, 45);
            this.textBoxHighlightModel.Size = new System.Drawing.Size(320, 20);
            this.textBoxHighlightModel.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.groupBoxColoring.Controls.Add(this.labelPinkReg);
            this.groupBoxColoring.Controls.Add(this.textBoxPinkReg);
            this.groupBoxColoring.Controls.Add(this.labelHighlightModel);
            this.groupBoxColoring.Controls.Add(this.textBoxHighlightModel);
            this.tabSettings.Controls.Add(this.groupBoxColoring);

            // === DOWNLOADS TAB ===
            this.tabDownloads.Text = "Downloads";
            this.tabDownloads.AutoScroll = true;
            this.tabDownloads.Padding = new System.Windows.Forms.Padding(6);
            int y = 8;
            int gbH = 68;
            int gap = 4;

            // Helper: create download group
            System.Action<System.Windows.Forms.GroupBox, string, System.Windows.Forms.Button, System.Windows.Forms.ProgressBar, System.Windows.Forms.Label, System.Windows.Forms.Label, System.Windows.Forms.Label, System.Windows.Forms.Label, System.Windows.Forms.NumericUpDown> setupDlGroup =
                (gb, title, btn, prog, status, lastLbl, lastVal, intLbl, intNum) => {
                gb.Location = new System.Drawing.Point(8, y);
                gb.Size = new System.Drawing.Size(530, gbH);
                gb.Text = title;
                btn.Location = new System.Drawing.Point(10, 18);
                btn.Size = new System.Drawing.Size(90, 23);
                btn.Text = "Download";
                prog.Location = new System.Drawing.Point(108, 18);
                prog.Size = new System.Drawing.Size(200, 23);
                status.AutoSize = true;
                status.Location = new System.Drawing.Point(315, 22);
                lastLbl.AutoSize = true;
                lastLbl.Location = new System.Drawing.Point(10, 46);
                lastLbl.Text = "Last:";
                lastVal.AutoSize = true;
                lastVal.Location = new System.Drawing.Point(42, 46);
                lastVal.Text = "Never";
                intLbl.AutoSize = true;
                intLbl.Location = new System.Drawing.Point(200, 46);
                intLbl.Text = "Interval (days):";
                intNum.Location = new System.Drawing.Point(295, 44);
                intNum.Size = new System.Drawing.Size(55, 20);
                intNum.Minimum = 1;
                intNum.Maximum = 365;
                gb.Controls.AddRange(new System.Windows.Forms.Control[] { btn, prog, status, lastLbl, lastVal, intLbl, intNum });
                this.tabDownloads.Controls.Add(gb);
                y += gbH + gap;
            };

            // FAA Aircraft
            this.buttonDownloadAircraft.Click += new System.EventHandler(this.buttonDownloadAircraft_Click);
            this.numericAircraftInterval.Value = 7;
            this.labelLastAircraftLabel = new System.Windows.Forms.Label();
            setupDlGroup(this.groupBoxAircraftDownload, "FAA Aircraft", this.buttonDownloadAircraft, this.progressBarAircraft, this.labelAircraftStatus, this.labelLastAircraftLabel, this.labelLastAircraft, this.labelAircraftInterval, this.numericAircraftInterval);

            // FAA Airmen
            this.labelLastAirmenLabel = new System.Windows.Forms.Label();
            this.buttonDownloadAirmen.Click += new System.EventHandler(this.buttonDownloadAirmen_Click);
            this.numericAirmenInterval.Value = 30;
            setupDlGroup(this.groupBoxAirmenDownload, "FAA Airmen", this.buttonDownloadAirmen, this.progressBarAirmen, this.labelAirmenStatus, this.labelLastAirmenLabel, this.labelLastAirmen, this.labelAirmenInterval, this.numericAirmenInterval);

            // CCAR
            this.buttonDownloadCcar.Click += new System.EventHandler(this.buttonDownloadCcar_Click);
            this.numericCcarInterval.Value = 30;
            setupDlGroup(this.groupBoxCcarDownload, "CCAR (Canada)", this.buttonDownloadCcar, this.progressBarCcar, this.labelCcarStatus, this.labelLastCcarLabel, this.labelLastCcar, this.labelCcarIntervalLabel, this.numericCcarInterval);

            // NTSB
            this.buttonDownloadNtsb.Click += new System.EventHandler(this.buttonDownloadNtsb_Click);
            this.numericNtsbInterval.Value = 90;
            setupDlGroup(this.groupBoxNtsbDownload, "NTSB Accidents", this.buttonDownloadNtsb, this.progressBarNtsb, this.labelNtsbStatus, this.labelLastNtsbLabel, this.labelLastNtsb, this.labelNtsbIntervalLabel, this.numericNtsbInterval);

            // SDR
            this.buttonDownloadSdr.Click += new System.EventHandler(this.buttonDownloadSdr_Click);
            this.numericSdrInterval.Value = 90;
            this.labelSdrIntervalLabel = new System.Windows.Forms.Label();
            setupDlGroup(this.groupBoxSdrDownload, "FAA SDR", this.buttonDownloadSdr, this.progressBarSdr, this.labelSdrStatus, this.labelLastSdrLabel, this.labelLastSdr, this.labelSdrIntervalLabel, this.numericSdrInterval);

            // CASA
            this.buttonDownloadCasa.Click += new System.EventHandler(this.buttonDownloadCasa_Click);
            this.numericCasaInterval.Value = 30;
            this.labelCasaIntervalLabel = new System.Windows.Forms.Label();
            setupDlGroup(this.groupBoxCasaDownload, "CASA (Australia)", this.buttonDownloadCasa, this.progressBarCasa, this.labelCasaStatus, this.labelLastCasaLabel, this.labelLastCasa, this.labelCasaIntervalLabel, this.numericCasaInterval);

            // NZ CAA (manual import)
            this.groupBoxNzcaa.Location = new System.Drawing.Point(8, y);
            this.groupBoxNzcaa.Size = new System.Drawing.Size(530, 48);
            this.groupBoxNzcaa.Text = "CAA (New Zealand)";
            this.buttonNzcaaPage.Location = new System.Drawing.Point(10, 18);
            this.buttonNzcaaPage.Size = new System.Drawing.Size(110, 23);
            this.buttonNzcaaPage.Text = "Download Page";
            this.buttonNzcaaPage.Click += new System.EventHandler(this.buttonNzcaaPage_Click);
            this.buttonNzcaaImport.Location = new System.Drawing.Point(128, 18);
            this.buttonNzcaaImport.Size = new System.Drawing.Size(70, 23);
            this.buttonNzcaaImport.Text = "Import";
            this.buttonNzcaaImport.Click += new System.EventHandler(this.buttonNzcaaImport_Click);
            this.labelLastNzcaa.AutoSize = true;
            this.labelLastNzcaa.Location = new System.Drawing.Point(210, 22);
            this.labelLastNzcaa.Text = "Last import: Never";
            this.groupBoxNzcaa.Controls.Add(this.buttonNzcaaPage);
            this.groupBoxNzcaa.Controls.Add(this.buttonNzcaaImport);
            this.groupBoxNzcaa.Controls.Add(this.labelLastNzcaa);
            this.tabDownloads.Controls.Add(this.groupBoxNzcaa);
            y += 52 + gap;

            // URLs group
            this.groupBoxUrls.Location = new System.Drawing.Point(8, y);
            this.groupBoxUrls.Size = new System.Drawing.Size(530, 140);
            this.groupBoxUrls.Text = "Download URLs (leave blank for defaults)";
            int uy = 18;
            string[] urlLabels = { "Aircraft:", "Airmen:", "CCAR:", "NTSB:", "CASA:" };
            System.Windows.Forms.Label[] urlLbls = new System.Windows.Forms.Label[] { this.labelAircraftUrl = new System.Windows.Forms.Label(), this.labelAirmenUrl = new System.Windows.Forms.Label(), this.labelCcarUrl = new System.Windows.Forms.Label(), this.labelNtsbUrl = new System.Windows.Forms.Label(), this.labelCasaUrl };
            System.Windows.Forms.TextBox[] urlTbs = new System.Windows.Forms.TextBox[] { this.textBoxAircraftUrl = new System.Windows.Forms.TextBox(), this.textBoxAirmenUrl = new System.Windows.Forms.TextBox(), this.textBoxCcarUrl = new System.Windows.Forms.TextBox(), this.textBoxNtsbUrl = new System.Windows.Forms.TextBox(), this.textBoxCasaUrl };
            for(int i = 0; i < 5; i++) {
                urlLbls[i].AutoSize = true;
                urlLbls[i].Location = new System.Drawing.Point(6, uy + 3);
                urlLbls[i].Text = urlLabels[i];
                urlTbs[i].Location = new System.Drawing.Point(60, uy);
                urlTbs[i].Size = new System.Drawing.Size(460, 20);
                this.groupBoxUrls.Controls.Add(urlLbls[i]);
                this.groupBoxUrls.Controls.Add(urlTbs[i]);
                uy += 24;
            }
            this.tabDownloads.Controls.Add(this.groupBoxUrls);
            y += 144 + gap;

            // Database stats
            this.labelDbStats.Location = new System.Drawing.Point(8, y);
            this.labelDbStats.Size = new System.Drawing.Size(530, 40);
            this.labelDbStats.Text = "Database not initialized.";
            this.tabDownloads.Controls.Add(this.labelDbStats);

            // === OK / Cancel ===
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(427, 525);
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(508, 525);
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.Text = "Cancel";

            // === FORM ===
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(595, 560);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Registration Data Options";

            this.tabDownloads.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.TabPage tabDownloads;
        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label labelFolderLabel;
        private System.Windows.Forms.TextBox textBoxDatabaseFolder;
        private System.Windows.Forms.Button buttonBrowseFolder;
        private System.Windows.Forms.GroupBox groupBoxDisplay;
        private System.Windows.Forms.RadioButton radioNewTab;
        private System.Windows.Forms.RadioButton radioPopup;
        private System.Windows.Forms.CheckBox checkBoxFetchPhotos;
        private System.Windows.Forms.CheckBox checkBoxDdgFallback;
        private System.Windows.Forms.GroupBox groupBoxPilot;
        private System.Windows.Forms.Label labelFuzzyLabel;
        private System.Windows.Forms.NumericUpDown numericFuzzyDistance;
        private System.Windows.Forms.Label labelPilotThreshold;
        private System.Windows.Forms.NumericUpDown numericPilotThreshold;
        private System.Windows.Forms.GroupBox groupBoxColoring;
        private System.Windows.Forms.Label labelPinkReg;
        private System.Windows.Forms.TextBox textBoxPinkReg;
        private System.Windows.Forms.Label labelHighlightModel;
        private System.Windows.Forms.TextBox textBoxHighlightModel;
        private System.Windows.Forms.GroupBox groupBoxAircraftDownload;
        private System.Windows.Forms.Button buttonDownloadAircraft;
        private System.Windows.Forms.ProgressBar progressBarAircraft;
        private System.Windows.Forms.Label labelAircraftStatus;
        private System.Windows.Forms.Label labelLastAircraftLabel;
        private System.Windows.Forms.Label labelLastAircraft;
        private System.Windows.Forms.Label labelAircraftInterval;
        private System.Windows.Forms.NumericUpDown numericAircraftInterval;
        private System.Windows.Forms.GroupBox groupBoxAirmenDownload;
        private System.Windows.Forms.Button buttonDownloadAirmen;
        private System.Windows.Forms.ProgressBar progressBarAirmen;
        private System.Windows.Forms.Label labelAirmenStatus;
        private System.Windows.Forms.Label labelLastAirmenLabel;
        private System.Windows.Forms.Label labelLastAirmen;
        private System.Windows.Forms.Label labelAirmenInterval;
        private System.Windows.Forms.NumericUpDown numericAirmenInterval;
        private System.Windows.Forms.GroupBox groupBoxCcarDownload;
        private System.Windows.Forms.Button buttonDownloadCcar;
        private System.Windows.Forms.ProgressBar progressBarCcar;
        private System.Windows.Forms.Label labelCcarStatus;
        private System.Windows.Forms.Label labelLastCcarLabel;
        private System.Windows.Forms.Label labelLastCcar;
        private System.Windows.Forms.Label labelCcarIntervalLabel;
        private System.Windows.Forms.NumericUpDown numericCcarInterval;
        private System.Windows.Forms.GroupBox groupBoxNtsbDownload;
        private System.Windows.Forms.Button buttonDownloadNtsb;
        private System.Windows.Forms.ProgressBar progressBarNtsb;
        private System.Windows.Forms.Label labelNtsbStatus;
        private System.Windows.Forms.Label labelLastNtsbLabel;
        private System.Windows.Forms.Label labelLastNtsb;
        private System.Windows.Forms.Label labelNtsbIntervalLabel;
        private System.Windows.Forms.NumericUpDown numericNtsbInterval;
        private System.Windows.Forms.GroupBox groupBoxSdrDownload;
        private System.Windows.Forms.Button buttonDownloadSdr;
        private System.Windows.Forms.ProgressBar progressBarSdr;
        private System.Windows.Forms.Label labelSdrStatus;
        private System.Windows.Forms.Label labelLastSdrLabel;
        private System.Windows.Forms.Label labelLastSdr;
        private System.Windows.Forms.Label labelSdrIntervalLabel;
        private System.Windows.Forms.NumericUpDown numericSdrInterval;
        private System.Windows.Forms.GroupBox groupBoxCasaDownload;
        private System.Windows.Forms.Button buttonDownloadCasa;
        private System.Windows.Forms.ProgressBar progressBarCasa;
        private System.Windows.Forms.Label labelCasaStatus;
        private System.Windows.Forms.Label labelLastCasaLabel;
        private System.Windows.Forms.Label labelLastCasa;
        private System.Windows.Forms.Label labelCasaIntervalLabel;
        private System.Windows.Forms.NumericUpDown numericCasaInterval;
        private System.Windows.Forms.Label labelCasaUrl;
        private System.Windows.Forms.TextBox textBoxCasaUrl;
        private System.Windows.Forms.GroupBox groupBoxNzcaa;
        private System.Windows.Forms.Button buttonNzcaaPage;
        private System.Windows.Forms.Button buttonNzcaaImport;
        private System.Windows.Forms.Label labelLastNzcaa;
        private System.Windows.Forms.GroupBox groupBoxUrls;
        private System.Windows.Forms.Label labelAircraftUrl;
        private System.Windows.Forms.TextBox textBoxAircraftUrl;
        private System.Windows.Forms.Label labelAirmenUrl;
        private System.Windows.Forms.TextBox textBoxAirmenUrl;
        private System.Windows.Forms.Label labelCcarUrl;
        private System.Windows.Forms.TextBox textBoxCcarUrl;
        private System.Windows.Forms.Label labelNtsbUrl;
        private System.Windows.Forms.TextBox textBoxNtsbUrl;
        private System.Windows.Forms.Label labelDbStats;
        private System.Windows.Forms.Label labelWeightUnit;
        private System.Windows.Forms.ComboBox comboWeightUnit;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
