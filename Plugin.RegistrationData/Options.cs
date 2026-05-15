using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualRadar.Plugin.RegistrationData
{
    public class Options : INotifyPropertyChanged
    {
        public long DataVersion { get; set; }

        private bool _Enabled = true;
        public bool Enabled
        {
            get { return _Enabled; }
            set { SetField(ref _Enabled, value, nameof(Enabled)); }
        }

        private string _DatabaseFolder;
        public string DatabaseFolder
        {
            get { return _DatabaseFolder; }
            set { SetField(ref _DatabaseFolder, value, nameof(DatabaseFolder)); }
        }

        private bool _OpenInNewTab = true;
        public bool OpenInNewTab
        {
            get { return _OpenInNewTab; }
            set { SetField(ref _OpenInNewTab, value, nameof(OpenInNewTab)); }
        }

        private int _AircraftUpdateIntervalDays = 7;
        public int AircraftUpdateIntervalDays
        {
            get { return _AircraftUpdateIntervalDays; }
            set { SetField(ref _AircraftUpdateIntervalDays, value, nameof(AircraftUpdateIntervalDays)); }
        }

        private int _AirmenUpdateIntervalDays = 30;
        public int AirmenUpdateIntervalDays
        {
            get { return _AirmenUpdateIntervalDays; }
            set { SetField(ref _AirmenUpdateIntervalDays, value, nameof(AirmenUpdateIntervalDays)); }
        }

        private DateTime _LastAircraftDownload = DateTime.MinValue;
        public DateTime LastAircraftDownload
        {
            get { return _LastAircraftDownload; }
            set { SetField(ref _LastAircraftDownload, value, nameof(LastAircraftDownload)); }
        }

        private DateTime _LastAirmenDownload = DateTime.MinValue;
        public DateTime LastAirmenDownload
        {
            get { return _LastAirmenDownload; }
            set { SetField(ref _LastAirmenDownload, value, nameof(LastAirmenDownload)); }
        }

        private int _PilotMatchThreshold = 120;
        public int PilotMatchThreshold
        {
            get { return _PilotMatchThreshold; }
            set { SetField(ref _PilotMatchThreshold, value, nameof(PilotMatchThreshold)); }
        }

        private int _FuzzyMatchMaxDistance = 2;
        public int FuzzyMatchMaxDistance
        {
            get { return _FuzzyMatchMaxDistance; }
            set { SetField(ref _FuzzyMatchMaxDistance, value, nameof(FuzzyMatchMaxDistance)); }
        }

        private int _CcarUpdateIntervalDays = 30;
        public int CcarUpdateIntervalDays
        {
            get { return _CcarUpdateIntervalDays; }
            set { SetField(ref _CcarUpdateIntervalDays, value, nameof(CcarUpdateIntervalDays)); }
        }

        private DateTime _LastCcarDownload = DateTime.MinValue;
        public DateTime LastCcarDownload
        {
            get { return _LastCcarDownload; }
            set { SetField(ref _LastCcarDownload, value, nameof(LastCcarDownload)); }
        }

        private bool _FetchAircraftPhotos = true;
        public bool FetchAircraftPhotos
        {
            get { return _FetchAircraftPhotos; }
            set { SetField(ref _FetchAircraftPhotos, value, nameof(FetchAircraftPhotos)); }
        }

        private bool _DuckDuckGoImageFallback = true;
        public bool DuckDuckGoImageFallback
        {
            get { return _DuckDuckGoImageFallback; }
            set { SetField(ref _DuckDuckGoImageFallback, value, nameof(DuckDuckGoImageFallback)); }
        }

        private string _AircraftDownloadUrl = "";
        public string AircraftDownloadUrl
        {
            get { return _AircraftDownloadUrl; }
            set { SetField(ref _AircraftDownloadUrl, value, nameof(AircraftDownloadUrl)); }
        }

        private string _AirmenDownloadUrl = "";
        public string AirmenDownloadUrl
        {
            get { return _AirmenDownloadUrl; }
            set { SetField(ref _AirmenDownloadUrl, value, nameof(AirmenDownloadUrl)); }
        }

        private string _CcarDownloadUrl = "";
        public string CcarDownloadUrl
        {
            get { return _CcarDownloadUrl; }
            set { SetField(ref _CcarDownloadUrl, value, nameof(CcarDownloadUrl)); }
        }

        private string _NtsbDownloadUrl = "";
        public string NtsbDownloadUrl
        {
            get { return _NtsbDownloadUrl; }
            set { SetField(ref _NtsbDownloadUrl, value, nameof(NtsbDownloadUrl)); }
        }

        private string _CasaDownloadUrl = "";
        public string CasaDownloadUrl
        {
            get { return _CasaDownloadUrl; }
            set { SetField(ref _CasaDownloadUrl, value, nameof(CasaDownloadUrl)); }
        }

        private bool _IncludeStateMatch = true;
        public bool IncludeStateMatch
        {
            get { return _IncludeStateMatch; }
            set { SetField(ref _IncludeStateMatch, value, nameof(IncludeStateMatch)); }
        }

        private int _NtsbUpdateIntervalDays = 90;
        public int NtsbUpdateIntervalDays
        {
            get { return _NtsbUpdateIntervalDays; }
            set { SetField(ref _NtsbUpdateIntervalDays, value, nameof(NtsbUpdateIntervalDays)); }
        }

        private DateTime _LastNtsbDownload = DateTime.MinValue;
        public DateTime LastNtsbDownload
        {
            get { return _LastNtsbDownload; }
            set { SetField(ref _LastNtsbDownload, value, nameof(LastNtsbDownload)); }
        }

        private int _SdrUpdateIntervalDays = 90;
        public int SdrUpdateIntervalDays
        {
            get { return _SdrUpdateIntervalDays; }
            set { SetField(ref _SdrUpdateIntervalDays, value, nameof(SdrUpdateIntervalDays)); }
        }

        private DateTime _LastSdrDownload = DateTime.MinValue;
        public DateTime LastSdrDownload
        {
            get { return _LastSdrDownload; }
            set { SetField(ref _LastSdrDownload, value, nameof(LastSdrDownload)); }
        }

        private int _CasaUpdateIntervalDays = 180;
        public int CasaUpdateIntervalDays
        {
            get { return _CasaUpdateIntervalDays; }
            set { SetField(ref _CasaUpdateIntervalDays, value, nameof(CasaUpdateIntervalDays)); }
        }

        private DateTime _LastCasaDownload = DateTime.MinValue;
        public DateTime LastCasaDownload
        {
            get { return _LastCasaDownload; }
            set { SetField(ref _LastCasaDownload, value, nameof(LastCasaDownload)); }
        }

        private int _NzcaaUpdateIntervalDays = 180;
        public int NzcaaUpdateIntervalDays
        {
            get { return _NzcaaUpdateIntervalDays; }
            set { SetField(ref _NzcaaUpdateIntervalDays, value, nameof(NzcaaUpdateIntervalDays)); }
        }

        private DateTime _LastNzcaaDownload = DateTime.MinValue;
        public DateTime LastNzcaaDownload
        {
            get { return _LastNzcaaDownload; }
            set { SetField(ref _LastNzcaaDownload, value, nameof(LastNzcaaDownload)); }
        }

        private string _WeightUnit = "lbs";  // "lbs" or "kg"
        public string WeightUnit
        {
            get { return _WeightUnit; }
            set { SetField(ref _WeightUnit, value, nameof(WeightUnit)); }
        }

        private string _PinkRegistration = "";
        public string PinkRegistration
        {
            get { return _PinkRegistration; }
            set { SetField(ref _PinkRegistration, value, nameof(PinkRegistration)); }
        }

        private string _HighlightModelIcao = "";
        public string HighlightModelIcao
        {
            get { return _HighlightModelIcao; }
            set { SetField(ref _HighlightModelIcao, value, nameof(HighlightModelIcao)); }
        }

        private string _ColorPriority = "pink,mdl,pilot,ntsb,sdr";
        public string ColorPriority
        {
            get { return _ColorPriority; }
            set { SetField(ref _ColorPriority, value, nameof(ColorPriority)); }
        }

        private bool _ShowPilotColor = true;
        public bool ShowPilotColor
        {
            get { return _ShowPilotColor; }
            set { SetField(ref _ShowPilotColor, value, nameof(ShowPilotColor)); }
        }

        private bool _ShowNtsbColor = true;
        public bool ShowNtsbColor
        {
            get { return _ShowNtsbColor; }
            set { SetField(ref _ShowNtsbColor, value, nameof(ShowNtsbColor)); }
        }

        private bool _ShowSdrColor = true;
        public bool ShowSdrColor
        {
            get { return _ShowSdrColor; }
            set { SetField(ref _ShowSdrColor, value, nameof(ShowSdrColor)); }
        }

        private bool _ShowModelColor = true;
        public bool ShowModelColor
        {
            get { return _ShowModelColor; }
            set { SetField(ref _ShowModelColor, value, nameof(ShowModelColor)); }
        }

        private bool _ShowLaddIndicator = true;
        public bool ShowLaddIndicator
        {
            get { return _ShowLaddIndicator; }
            set { SetField(ref _ShowLaddIndicator, value, nameof(ShowLaddIndicator)); }
        }

        private string _LaddColor = "#e67e22";
        public string LaddColor
        {
            get { return _LaddColor; }
            set { SetField(ref _LaddColor, value, nameof(LaddColor)); }
        }

        private string _RowColorMode = "row";  // "row", "reg", "call", "reg,call"
        public string RowColorMode
        {
            get { return _RowColorMode; }
            set { SetField(ref _RowColorMode, value, nameof(RowColorMode)); }
        }

        private string _ModelRowColorMode = "row";  // "row", "reg", "call", "mdl", combos
        public string ModelRowColorMode
        {
            get { return _ModelRowColorMode; }
            set { SetField(ref _ModelRowColorMode, value, nameof(ModelRowColorMode)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            var handler = PropertyChanged;
            if(handler != null) {
                handler(this, args);
            }
        }

        protected bool SetField<T>(ref T field, T value, string fieldName)
        {
            var result = !EqualityComparer<T>.Default.Equals(field, value);
            if(result) {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(fieldName));
            }
            return result;
        }
    }
}
