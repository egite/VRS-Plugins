using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualRadar.Plugin.SnapToOwnship
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

        private string _OwnshipIcao;
        public string OwnshipIcao
        {
            get { return _OwnshipIcao; }
            set { SetField(ref _OwnshipIcao, value, nameof(OwnshipIcao)); }
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
            OwnshipIcao = "";
        }
    }
}
