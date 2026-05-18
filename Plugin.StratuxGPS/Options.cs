using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualRadar.Plugin.StratuxGPS
{
    /// <summary>
    /// The plugin's options.
    /// </summary>
    public class Options : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating the version of the saved options.
        /// </summary>
        public long DataVersion { get; set; }

        private bool _Enabled;
        /// <summary>
        /// Gets or sets a value indicating whether the plugin is active.
        /// </summary>
        public bool Enabled
        {
            get { return _Enabled; }
            set { SetField(ref _Enabled, value, nameof(Enabled)); }
        }

        private string _StratuxAddress = "192.168.10.1";
        /// <summary>
        /// Gets or sets the IP address or hostname of the Stratux device.
        /// </summary>
        public string StratuxAddress
        {
            get { return _StratuxAddress; }
            set { SetField(ref _StratuxAddress, value, nameof(StratuxAddress)); }
        }

        private int _StratuxPort = 80;
        /// <summary>
        /// Gets or sets the port number for the Stratux HTTP API.
        /// </summary>
        public int StratuxPort
        {
            get { return _StratuxPort; }
            set { SetField(ref _StratuxPort, value, nameof(StratuxPort)); }
        }

        private int _PollIntervalMilliseconds = 1000;
        /// <summary>
        /// Gets or sets how often (in milliseconds) to poll the Stratux device for position updates.
        /// </summary>
        public int PollIntervalMilliseconds
        {
            get { return _PollIntervalMilliseconds; }
            set { SetField(ref _PollIntervalMilliseconds, value, nameof(PollIntervalMilliseconds)); }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/>.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            var handler = PropertyChanged;
            if(handler != null) {
                handler(this, args);
            }
        }

        /// <summary>
        /// Sets the field's value and raises <see cref="PropertyChanged"/>, but only when the value has changed.
        /// </summary>
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
