if(!VRS) var VRS = {};
if(!VRS.WebAdmin) VRS.WebAdmin = {};
if(!VRS.WebAdmin.StratuxGPSPluginOptions) VRS.WebAdmin.StratuxGPSPluginOptions = {};

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler = function(viewId) {
    var self = this;
    self._ViewId = new VRS.WebAdmin.ViewId('StratuxGPSPluginOptions', viewId);
    self._Model = null;
    self.refreshState();
};

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.showFailureMessage = function(message) {
    var alert = $('#failure-message');
    if(message && message.length) {
        alert.text(message || '').show();
    } else {
        alert.hide();
    }
};

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.refreshState = function() {
    var self = this;
    self.showFailureMessage(null);
    self._ViewId.ajax('GetState', {
        success: function(data) {
            self.applyState(data);
        },
        error: function() {
            setTimeout(function() { self.refreshState(); }, 5000);
        }
    }, false);
};

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.save = function() {
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

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.buildAjaxSettingsForSendConfiguration = function() {
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

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.applyState = function(state) {
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
                        root.PositionStatus = ko.observable('Waiting for first poll…');
                        root.PositionLat = ko.observable('—');
                        root.PositionLng = ko.observable('—');
                        root.PositionAlt = ko.observable('—');
                        root.PositionSpd = ko.observable('—');
                    }
                }
            });
            ko.applyBindings(self._Model);
            self.startPositionPolling();
        }
    }
};

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.startPositionPolling = function() {
    var self = this;
    if(self._PositionTimer) return;

    var pollPosition = function() {
        self._ViewId.ajax('GetPosition', {
            success: function(data) {
                if(data && !data.Exception && data.Response) {
                    self.applyPosition(data.Response);
                }
            },
            error: function() { /* keep polling; transient errors ignored */ }
        }, false);
    };

    var schedule = function() {
        var interval = 1000;
        if(self._Model && typeof self._Model.PollIntervalMilliseconds === 'function') {
            var v = parseInt(self._Model.PollIntervalMilliseconds(), 10);
            if(!isNaN(v) && v >= 250) interval = v;
        }
        self._PositionTimer = setTimeout(function() {
            pollPosition();
            schedule();
        }, interval);
    };

    pollPosition();
    schedule();
};

VRS.WebAdmin.StratuxGPSPluginOptions.PageHandler.prototype.applyPosition = function(pos) {
    var self = this;
    if(!self._Model) return;

    if(!pos.PluginRunning) {
        self._Model.PositionStatus('Plugin not running');
        self._Model.PositionLat('—');
        self._Model.PositionLng('—');
        self._Model.PositionAlt('—');
        self._Model.PositionSpd('—');
    } else if(!pos.HasPosition) {
        self._Model.PositionStatus('Waiting for GPS fix…');
        self._Model.PositionLat('—');
        self._Model.PositionLng('—');
        self._Model.PositionAlt('—');
        self._Model.PositionSpd('—');
    } else {
        self._Model.PositionStatus('GPS fix (' + pos.FixQuality + '), age ' + pos.AgeSeconds.toFixed(1) + 's');
        self._Model.PositionLat(pos.Latitude.toFixed(6) + '°');
        self._Model.PositionLng(pos.Longitude.toFixed(6) + '°');
        self._Model.PositionAlt(pos.AltitudeFeet.toFixed(0) + ' ft');
        self._Model.PositionSpd(pos.GroundSpeedKnots.toFixed(1) + ' kts');
    }
};
