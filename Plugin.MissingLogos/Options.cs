using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace VirtualRadar.Plugin.MissingLogos
{
    public class Options : INotifyPropertyChanged
    {
        public long DataVersion { get; set; }

        private bool _Enabled;
        public bool Enabled
        {
            get { return _Enabled; }
            set { SetField(ref _Enabled, value, nameof(Enabled)); }
        }

        private string _LogFileName = "";
        public string LogFileName
        {
            get { return _LogFileName; }
            set { SetField(ref _LogFileName, value, nameof(LogFileName)); }
        }

        private bool _TrackMissingModels;
        public bool TrackMissingModels
        {
            get { return _TrackMissingModels; }
            set { SetField(ref _TrackMissingModels, value, nameof(TrackMissingModels)); }
        }

        private string _ModelLogFileName = "";
        public string ModelLogFileName
        {
            get { return _ModelLogFileName; }
            set { SetField(ref _ModelLogFileName, value, nameof(ModelLogFileName)); }
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
