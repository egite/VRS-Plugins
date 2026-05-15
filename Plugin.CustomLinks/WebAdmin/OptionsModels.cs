using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualRadar.Plugin.CustomLinks.WebAdmin
{
    public class LinkModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string ConditionPrefix { get; set; }

        public LinkModel()
        {
        }
    }

    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }
        public List<LinkModel> Links { get; set; }

        public ViewModel()
        {
            Links = new List<LinkModel>();
        }

        public ViewModel(Options options) : this()
        {
            RefreshFromSettings(options);
        }

        public void RefreshFromSettings(Options options)
        {
            DataVersion = options.DataVersion;
            Enabled = options.Enabled;
            Links = (options.Links ?? new List<LinkDefinition>()).Select(l => new LinkModel {
                Name            = l.Name ?? "",
                Url             = l.Url ?? "",
                ConditionPrefix = l.ConditionPrefix ?? "",
            }).ToList();
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
            options.Links = (Links ?? new List<LinkModel>()).Select(m => new LinkDefinition {
                Name            = m.Name,
                Url             = m.Url,
                ConditionPrefix = m.ConditionPrefix,
            }).ToList();
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
