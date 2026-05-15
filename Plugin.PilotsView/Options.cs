using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualRadar.Plugin.PilotsView
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

        private int _RefreshInterval;
        public int RefreshInterval
        {
            get { return _RefreshInterval; }
            set { SetField(ref _RefreshInterval, value, nameof(RefreshInterval)); }
        }

        private int _CameraTilt;
        public int CameraTilt
        {
            get { return _CameraTilt; }
            set { SetField(ref _CameraTilt, value, nameof(CameraTilt)); }
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

        public Options()
        {
            RefreshInterval = 2;
            CameraTilt = 80;
        }
    }
}
