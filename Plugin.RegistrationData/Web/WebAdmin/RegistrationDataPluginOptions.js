if(!VRS) var VRS = {};
if(!VRS.WebAdmin) VRS.WebAdmin = {};
if(!VRS.WebAdmin.RegistrationDataPluginOptions) VRS.WebAdmin.RegistrationDataPluginOptions = {};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler = function(viewId) {
    var self = this;
    self._ViewId = new VRS.WebAdmin.ViewId('RegistrationDataPluginOptions', viewId);
    self._Model = null;
    self._PollTimer = null;
    self.refreshState();
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.showFailureMessage = function(message) {
    var alert = $('#failure-message');
    if(message && message.length) {
        alert.text(message || '').show();
    } else {
        alert.hide();
    }
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.refreshState = function() {
    var self = this;
    self.showFailureMessage(null);
    self._ViewId.ajax('GetState', {
        success: function(data) {
            self.applyState(data);
            self.startPollingIfDownloading();
        },
        error: function() {
            setTimeout(function() { self.refreshState(); }, 5000);
        }
    }, false);
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.startPollingIfDownloading = function() {
    var self = this;
    if(self._PollTimer) { clearInterval(self._PollTimer); self._PollTimer = null; }
    if(self._Model && self._Model.IsDownloading()) {
        self._PollTimer = setInterval(function() {
            self._ViewId.ajax('GetState', {
                success: function(data) {
                    if(data && !data.Exception && data.Response) {
                        ko.viewmodel.updateFromModel(self._Model, data.Response);
                        if(!self._Model.IsDownloading()) {
                            clearInterval(self._PollTimer);
                            self._PollTimer = null;
                        }
                    }
                }
            }, false);
        }, 2000);
    }
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.save = function() {
    var self = this;
    self._Model.SaveAttempted(false);
    var settings = self.buildAjaxSettingsForSendConfiguration();
    settings.success = function(data) {
        if(data.Exception) {
            self.showFailureMessage(VRS.stringUtility.format(VRS.WebAdmin.$$.WA_Exception_Reported, data.Exception));
            self._Model.SaveSuccessful(false);
        } else {
            if(data.Response && data.Response.Outcome) {
                self._Model.SaveAttempted(true);
                self._Model.SaveSuccessful(data.Response.Outcome === 'Saved');
                switch(data.Response.Outcome || '') {
                    case 'Saved':            self._Model.SavedMessage(VRS.WebAdmin.$$.WA_Saved); break;
                    case 'FailedValidation': self._Model.SavedMessage(VRS.WebAdmin.$$.WA_Validation_Failed); break;
                    case 'ConflictingUpdate':self._Model.SavedMessage(VRS.WebAdmin.$$.WA_Conflicting_Update); break;
                }
            }
            ko.viewmodel.updateFromModel(self._Model, data.Response.ViewModel);
        }
    };
    self._ViewId.ajax('Save', settings);
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadAircraft = function() { this.triggerDownload('DownloadAircraft'); };
VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadAirmen = function() { this.triggerDownload('DownloadAirmen'); };
VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadCcar = function() { this.triggerDownload('DownloadCcar'); };
VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadNtsb = function() { this.triggerDownload('DownloadNtsb'); };
VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadSdr = function() { this.triggerDownload('DownloadSdr'); };
VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadCasa = function() { this.triggerDownload('DownloadCasa'); };
VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.downloadNzcaa = function() { this.triggerDownload('DownloadNzcaa'); };

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.triggerDownload = function(method) {
    var self = this;
    self._ViewId.ajax(method, {
        success: function(data) {
            if(data && !data.Exception && data.Response) {
                ko.viewmodel.updateFromModel(self._Model, data.Response);
            }
            self.startPollingIfDownloading();
        }
    }, false);
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.buildAjaxSettingsForSendConfiguration = function() {
    var self = this;
    var viewModel = ko.viewmodel.toModel(self._Model);
    return {
        method: 'POST',
        data: { viewModel: JSON.stringify(viewModel) },
        dataType: 'json',
        error: function(jqXHR, textStatus, errorThrown) {
            self.showFailureMessage(VRS.stringUtility.format(VRS.WebAdmin.$$.WA_Send_Failed, errorThrown));
        }
    };
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.applyState = function(state) {
    var self = this;
    if(state.Exception) {
        self.showFailureMessage(VRS.stringUtility.format(VRS.WebAdmin.$$.WA_Exception_Reported, state.Exception));
    } else {
        self.showFailureMessage(null);
        if(self._Model) {
            ko.viewmodel.updateFromModel(self._Model, state.Response);
        } else {
            self._Model = ko.viewmodel.fromModel(state.Response, {
                arrayChildId: {},
                extend: {
                    '{root}': function(root) {
                        root.SaveAttempted = ko.observable(false);
                        root.SaveSuccessful = ko.observable(false);
                        root.SavedMessage = ko.observable('');
                        root.IncludeModelField = ko.pureComputed({
                            read: function() { return (root.ModelRowColorMode() || '').indexOf('mdl') >= 0; },
                            write: function(val) { root.ModelRowColorMode(val ? 'mdl' : 'none'); }
                        });
                    }
                }
            });
            ko.applyBindings(self._Model);
            self.renderPriorityList();
            self._Model.ColorPriority.subscribe(function() { self.renderPriorityList(); });
        }
    }
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.renderPriorityList = function() {
    var self = this;
    var labels = {pink:'Ownship',mdl:'Model',pilot:'Pilot',ntsb:'NTSB',sdr:'SDR'};
    var colors = {pink:'#FF69B4',mdl:'#5DADE2',pilot:'#00CC00',ntsb:'#FF0000',sdr:'#9B59B6'};
    var order = (self._Model.ColorPriority() || 'pink,mdl,pilot,ntsb,sdr').split(',');
    var el = document.getElementById('priority-list');
    if(!el) return;
    el.innerHTML = '';
    for(var i = 0; i < order.length; i++) {
        var key = order[i].trim();
        if(!labels[key]) continue;
        var row = document.createElement('div');
        row.style.cssText = 'display:inline-flex;align-items:center;margin:2px 4px 2px 0;padding:2px 6px;background:#f5f5f5;border:1px solid #ddd;border-radius:3px;';
        var badge = document.createElement('span');
        badge.className = 'label';
        badge.style.backgroundColor = colors[key];
        badge.textContent = labels[key];
        badge.style.marginRight = '4px';
        row.appendChild(badge);
        if(i > 0) {
            var upBtn = document.createElement('button');
            upBtn.type = 'button';
            upBtn.className = 'btn btn-xs btn-default';
            upBtn.innerHTML = '&#9650;';
            upBtn.style.marginRight = '2px';
            upBtn.setAttribute('data-idx', i);
            upBtn.onclick = function() { self.movePriority(parseInt(this.getAttribute('data-idx')), -1); };
            row.appendChild(upBtn);
        }
        if(i < order.length - 1) {
            var dnBtn = document.createElement('button');
            dnBtn.type = 'button';
            dnBtn.className = 'btn btn-xs btn-default';
            dnBtn.innerHTML = '&#9660;';
            dnBtn.setAttribute('data-idx', i);
            dnBtn.onclick = function() { self.movePriority(parseInt(this.getAttribute('data-idx')), 1); };
            row.appendChild(dnBtn);
        }
        el.appendChild(row);
    }
};

VRS.WebAdmin.RegistrationDataPluginOptions.PageHandler.prototype.movePriority = function(idx, dir) {
    var order = (this._Model.ColorPriority() || 'pink,mdl,pilot,ntsb,sdr').split(',');
    var newIdx = idx + dir;
    if(newIdx < 0 || newIdx >= order.length) return;
    var tmp = order[idx];
    order[idx] = order[newIdx];
    order[newIdx] = tmp;
    this._Model.ColorPriority(order.join(','));
};
