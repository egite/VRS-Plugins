using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using VirtualRadar.Interface.View;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.RegistrationData.WebAdmin
{
    class OptionsView : IView
    {
        public DialogResult ShowView() { return DialogResult.OK; }
        public void Dispose() { }

        private void PopulateStatus(ViewModel viewModel)
        {
            try {
                var db = Plugin.Singleton?.Database;
                if(db != null) {
                    var counts = db.GetRecordCounts();
                    var parts = new List<string>();
                    long v;
                    if(counts.TryGetValue("aircraft_registration", out v)) parts.Add($"Aircraft: {v:N0}");
                    if(counts.TryGetValue("airmen_basic", out v)) parts.Add($"Airmen: {v:N0}");
                    if(counts.TryGetValue("ccar_aircraft", out v)) parts.Add($"CCAR: {v:N0}");
                    if(counts.TryGetValue("ntsb_event", out v)) parts.Add($"NTSB: {v:N0}");
                    if(counts.TryGetValue("sdr_report", out v)) parts.Add($"SDR: {v:N0}");
                    if(counts.TryGetValue("casa_aircraft", out v)) parts.Add($"CASA: {v:N0}");
                    if(counts.TryGetValue("nzcaa_aircraft", out v)) parts.Add($"NZ CAA: {v:N0}");
                    parts.Add($"LADD: {Plugin.Singleton?.LaddCount ?? 0:N0} (Sep 2022)");
                    viewModel.DatabaseStats = string.Join(" | ", parts);
                }
            } catch {
                viewModel.DatabaseStats = "Unable to read database stats";
            }

            var dl = Plugin.Singleton?.Downloader;
            if(dl != null) {
                viewModel.IsDownloading = dl.IsDownloading;
                var status = new List<string>();
                if(dl.IsDownloadingAircraft) status.Add("Aircraft: " + (dl.AircraftPhase ?? ""));
                if(dl.IsDownloadingAirmen) status.Add("Airmen: " + (dl.AirmenPhase ?? ""));
                if(dl.IsDownloadingCcar) status.Add("CCAR: " + (Plugin.Singleton?.StatusDescription ?? ""));
                if(dl.IsDownloadingNtsb) status.Add("NTSB: " + (dl.NtsbPhase ?? ""));
                if(dl.IsDownloadingSdr) status.Add("SDR: " + (dl.SdrPhase ?? ""));
                if(dl.IsDownloadingCasa) status.Add("CASA: " + (dl.CasaPhase ?? ""));
                if(dl.IsDownloadingNzcaa) status.Add("NZ CAA: " + (dl.NzcaaPhase ?? ""));
                viewModel.DownloadStatus = status.Count > 0 ? string.Join("; ", status) : "Idle";
            }
        }

        [WebAdminMethod]
        public ViewModel GetState()
        {
            var options = OptionsStorage.Load(Plugin.Singleton);
            var viewModel = new ViewModel(options);
            PopulateStatus(viewModel);
            return viewModel;
        }

        [WebAdminMethod(DeferExecution = true)]
        public SaveOutcomeModel Save(ViewModel viewModel)
        {
            var outcome = "";

            var options = new Options();
            viewModel.CopyToSettings(options);

            var current = OptionsStorage.Load(Plugin.Singleton);
            options.LastAircraftDownload = current.LastAircraftDownload;
            options.LastAirmenDownload = current.LastAirmenDownload;
            options.LastCcarDownload = current.LastCcarDownload;
            options.LastNtsbDownload = current.LastNtsbDownload;
            options.LastSdrDownload = current.LastSdrDownload;
            options.LastCasaDownload = current.LastCasaDownload;
            options.LastNzcaaDownload = current.LastNzcaaDownload;

            try {
                OptionsStorage.Save(Plugin.Singleton, options);
                Plugin.Singleton?.ReloadOptions();
                outcome = "Saved";
            } catch(Exception) {
                outcome = "ConflictingUpdate";
            }

            options = OptionsStorage.Load(Plugin.Singleton);
            viewModel.RefreshFromSettings(options);
            PopulateStatus(viewModel);

            return new SaveOutcomeModel(outcome, viewModel);
        }

        [WebAdminMethod]
        public ViewModel DownloadAircraft()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadAircraftDatabase());
            return GetState();
        }

        [WebAdminMethod]
        public ViewModel DownloadAirmen()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadAirmenDatabase());
            return GetState();
        }

        [WebAdminMethod]
        public ViewModel DownloadCcar()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadCcarDatabase());
            return GetState();
        }

        [WebAdminMethod]
        public ViewModel DownloadNtsb()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadNtsbDatabase());
            return GetState();
        }

        [WebAdminMethod]
        public ViewModel DownloadSdr()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadSdrDatabase());
            return GetState();
        }

        [WebAdminMethod]
        public ViewModel DownloadNzcaa()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadNzcaaDatabase());
            return GetState();
        }

        [WebAdminMethod]
        public ViewModel DownloadCasa()
        {
            ThreadPool.QueueUserWorkItem(_ => Plugin.Singleton?.Downloader?.DownloadCasaDatabase());
            return GetState();
        }
    }
}
