using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualRadar.Plugin.TileServerMBTiles
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

        private string _FolderPath;
        /// <summary>
        /// Gets or sets the folder path containing .mbtiles files.
        /// Each .mbtiles file in this folder becomes a separate map option.
        /// </summary>
        public string FolderPath
        {
            get { return _FolderPath; }
            set { SetField(ref _FolderPath, value, nameof(FolderPath)); }
        }

        private bool _IsTms;
        /// <summary>
        /// Gets or sets whether the tiles use TMS coordinate scheme (Y axis flipped).
        /// MBTiles spec uses TMS by default.
        /// </summary>
        public bool IsTms
        {
            get { return _IsTms; }
            set { SetField(ref _IsTms, value, nameof(IsTms)); }
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
            FolderPath = "";
            IsTms = true;
        }
    }
}
