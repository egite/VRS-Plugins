using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VirtualRadar.WinForms;
using VirtualRadar.WinForms.PortableBinding;

namespace VirtualRadar.Plugin.MissingLogos.WinForms
{
    public partial class OptionsView : BaseForm
    {
        private Options _Options;
        public Options Options
        {
            get { return _Options; }
            set {
                if(_Options != value) {
                    _Options = value;
                }
            }
        }

        public OptionsView()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode) {
                ApplyBindings();
                InitialiseControlBinders();
            }
        }

        private void ApplyBindings()
        {
            AddControlBinder(new CheckBoxBoolBinder<Options>(Options, checkBoxEnabled,              r => r.Enabled,            (r,v) => r.Enabled = v));
            AddControlBinder(new TextBoxStringBinder<Options>(Options, textBoxLogFileName,           r => r.LogFileName,        (r,v) => r.LogFileName = v));
            AddControlBinder(new CheckBoxBoolBinder<Options>(Options, checkBoxTrackMissingModels,   r => r.TrackMissingModels, (r,v) => r.TrackMissingModels = v));
            AddControlBinder(new TextBoxStringBinder<Options>(Options, textBoxModelLogFileName,      r => r.ModelLogFileName,   (r,v) => r.ModelLogFileName = v));
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            BrowseForLogFile(textBoxLogFileName);
        }

        private void buttonBrowseModelLog_Click(object sender, EventArgs e)
        {
            BrowseForLogFile(textBoxModelLogFileName);
        }

        private void BrowseForLogFile(TextBox target)
        {
            using(var dialog = new SaveFileDialog()) {
                dialog.Title = "Log File Location";
                dialog.Filter = "Log files (*.log)|*.log|All files (*.*)|*.*";
                dialog.DefaultExt = "log";
                if(!String.IsNullOrEmpty(target.Text)) {
                    try {
                        dialog.InitialDirectory = System.IO.Path.GetDirectoryName(target.Text);
                        dialog.FileName = System.IO.Path.GetFileName(target.Text);
                    } catch { }
                }

                if(dialog.ShowDialog() == DialogResult.OK) {
                    target.Text = dialog.FileName;
                }
            }
        }
    }
}
