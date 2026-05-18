using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace VirtualRadar.Plugin.LogoMarkers
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

        private bool _Enabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether the plugin is active.
        /// </summary>
        public bool Enabled
        {
            get { return _Enabled; }
            set { SetField(ref _Enabled, value, nameof(Enabled)); }
        }

        private bool _ServerSideCompositing;
        /// <summary>
        /// Gets or sets a value indicating whether image compositing (heading
        /// arrow + logo) is performed on the server using System.Drawing (true)
        /// or in the browser via the Canvas API (false). Server-side mode
        /// requires libgdiplus on Linux/Mono.
        /// </summary>
        public bool ServerSideCompositing
        {
            get { return _ServerSideCompositing; }
            set { SetField(ref _ServerSideCompositing, value, nameof(ServerSideCompositing)); }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/>.
        /// </summary>
        /// <param name="args"></param>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="fieldName"></param>
        /// <returns>True if the value was set because it had changed, false if the value did not change and the event was not raised.</returns>
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
