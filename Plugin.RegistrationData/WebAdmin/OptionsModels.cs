using System;

namespace VirtualRadar.Plugin.RegistrationData.WebAdmin
{
    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }
        public string DatabaseFolder { get; set; }
        public bool OpenInNewTab { get; set; }
        public bool FetchAircraftPhotos { get; set; }
        public bool DuckDuckGoImageFallback { get; set; }
        public int AircraftUpdateIntervalDays { get; set; }
        public int AirmenUpdateIntervalDays { get; set; }
        public int CcarUpdateIntervalDays { get; set; }
        public int NtsbUpdateIntervalDays { get; set; }
        public int SdrUpdateIntervalDays { get; set; }
        public int CasaUpdateIntervalDays { get; set; }
        public int NzcaaUpdateIntervalDays { get; set; }
        public int FuzzyMatchMaxDistance { get; set; }
        public int PilotMatchThreshold { get; set; }
        public string PinkRegistration { get; set; }
        public string HighlightModelIcao { get; set; }
        public string ColorPriority { get; set; }
        public bool ShowPilotColor { get; set; }
        public bool ShowNtsbColor { get; set; }
        public bool ShowSdrColor { get; set; }
        public bool ShowModelColor { get; set; }
        public bool ShowLaddIndicator { get; set; }
        public string LaddColor { get; set; }
        public string WeightUnit { get; set; }
        public string RowColorMode { get; set; }
        public string ModelRowColorMode { get; set; }
        public string AircraftDownloadUrl { get; set; }
        public string AirmenDownloadUrl { get; set; }
        public string CcarDownloadUrl { get; set; }
        public string NtsbDownloadUrl { get; set; }
        public string CasaDownloadUrl { get; set; }
        public bool IncludeStateMatch { get; set; }

        // Read-only status info
        public string LastAircraftDownload { get; set; }
        public string LastAirmenDownload { get; set; }
        public string LastCcarDownload { get; set; }
        public string LastNtsbDownload { get; set; }
        public string LastSdrDownload { get; set; }
        public string LastCasaDownload { get; set; }
        public string LastNzcaaDownload { get; set; }
        public string DatabaseStats { get; set; }

        // Download status
        public bool IsDownloading { get; set; }
        public string DownloadStatus { get; set; }

        public ViewModel()
        {
        }

        public ViewModel(Options options) : this()
        {
            RefreshFromSettings(options);
        }

        public void RefreshFromSettings(Options options)
        {
            DataVersion = options.DataVersion;
            Enabled = options.Enabled;
            DatabaseFolder = options.DatabaseFolder ?? "";
            OpenInNewTab = options.OpenInNewTab;
            FetchAircraftPhotos = options.FetchAircraftPhotos;
            DuckDuckGoImageFallback = options.DuckDuckGoImageFallback;
            AircraftUpdateIntervalDays = options.AircraftUpdateIntervalDays;
            AirmenUpdateIntervalDays = options.AirmenUpdateIntervalDays;
            CcarUpdateIntervalDays = options.CcarUpdateIntervalDays;
            NtsbUpdateIntervalDays = options.NtsbUpdateIntervalDays;
            SdrUpdateIntervalDays = options.SdrUpdateIntervalDays;
            CasaUpdateIntervalDays = options.CasaUpdateIntervalDays;
            NzcaaUpdateIntervalDays = options.NzcaaUpdateIntervalDays;
            FuzzyMatchMaxDistance = options.FuzzyMatchMaxDistance;
            PilotMatchThreshold = options.PilotMatchThreshold;
            PinkRegistration = options.PinkRegistration ?? "";
            HighlightModelIcao = options.HighlightModelIcao ?? "";
            ColorPriority = options.ColorPriority ?? "pink,mdl,pilot,ntsb,sdr";
            ShowPilotColor = options.ShowPilotColor;
            ShowNtsbColor = options.ShowNtsbColor;
            ShowSdrColor = options.ShowSdrColor;
            ShowModelColor = options.ShowModelColor;
            ShowLaddIndicator = options.ShowLaddIndicator;
            LaddColor = options.LaddColor ?? "#e67e22";
            WeightUnit = options.WeightUnit ?? "lbs";
            RowColorMode = options.RowColorMode ?? "row";
            ModelRowColorMode = options.ModelRowColorMode ?? "row";
            AircraftDownloadUrl = options.AircraftDownloadUrl ?? "";
            AirmenDownloadUrl = options.AirmenDownloadUrl ?? "";
            CcarDownloadUrl = options.CcarDownloadUrl ?? "";
            NtsbDownloadUrl = options.NtsbDownloadUrl ?? "";
            CasaDownloadUrl = options.CasaDownloadUrl ?? "";
            IncludeStateMatch = options.IncludeStateMatch;

            LastAircraftDownload = options.LastAircraftDownload == DateTime.MinValue ? "Never" : options.LastAircraftDownload.ToString("g");
            LastAirmenDownload = options.LastAirmenDownload == DateTime.MinValue ? "Never" : options.LastAirmenDownload.ToString("g");
            LastCcarDownload = options.LastCcarDownload == DateTime.MinValue ? "Never" : options.LastCcarDownload.ToString("g");
            LastNtsbDownload = options.LastNtsbDownload == DateTime.MinValue ? "Never" : options.LastNtsbDownload.ToString("g");
            LastSdrDownload = options.LastSdrDownload == DateTime.MinValue ? "Never" : options.LastSdrDownload.ToString("g");
            LastCasaDownload = options.LastCasaDownload == DateTime.MinValue ? "Never" : options.LastCasaDownload.ToString("g");
            LastNzcaaDownload = options.LastNzcaaDownload == DateTime.MinValue ? "Never" : options.LastNzcaaDownload.ToString("g");
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
            options.DatabaseFolder = DatabaseFolder;
            options.OpenInNewTab = OpenInNewTab;
            options.FetchAircraftPhotos = FetchAircraftPhotos;
            options.DuckDuckGoImageFallback = DuckDuckGoImageFallback;
            options.AircraftUpdateIntervalDays = AircraftUpdateIntervalDays;
            options.AirmenUpdateIntervalDays = AirmenUpdateIntervalDays;
            options.CcarUpdateIntervalDays = CcarUpdateIntervalDays;
            options.NtsbUpdateIntervalDays = NtsbUpdateIntervalDays;
            options.SdrUpdateIntervalDays = SdrUpdateIntervalDays;
            options.CasaUpdateIntervalDays = CasaUpdateIntervalDays;
            options.NzcaaUpdateIntervalDays = NzcaaUpdateIntervalDays;
            options.FuzzyMatchMaxDistance = FuzzyMatchMaxDistance;
            options.PilotMatchThreshold = PilotMatchThreshold;
            options.PinkRegistration = PinkRegistration;
            options.HighlightModelIcao = HighlightModelIcao;
            options.ColorPriority = ColorPriority;
            options.ShowPilotColor = ShowPilotColor;
            options.ShowNtsbColor = ShowNtsbColor;
            options.ShowSdrColor = ShowSdrColor;
            options.ShowModelColor = ShowModelColor;
            options.ShowLaddIndicator = ShowLaddIndicator;
            options.LaddColor = LaddColor;
            options.WeightUnit = WeightUnit;
            options.RowColorMode = RowColorMode;
            options.ModelRowColorMode = ModelRowColorMode;
            options.AircraftDownloadUrl = AircraftDownloadUrl;
            options.AirmenDownloadUrl = AirmenDownloadUrl;
            options.CcarDownloadUrl = CcarDownloadUrl;
            options.NtsbDownloadUrl = NtsbDownloadUrl;
            options.CasaDownloadUrl = CasaDownloadUrl;
            options.IncludeStateMatch = IncludeStateMatch;
        }
    }

    public class SaveOutcomeModel
    {
        public string Outcome { get; set; }
        public ViewModel ViewModel { get; set; }

        public SaveOutcomeModel(string outcome, ViewModel viewModel)
        {
            Outcome = outcome;
            ViewModel = viewModel;
        }
    }
}
