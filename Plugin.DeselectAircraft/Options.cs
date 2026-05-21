using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualRadar.Plugin.DeselectAircraft
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
