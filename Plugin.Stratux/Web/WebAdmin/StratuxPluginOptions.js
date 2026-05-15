if(!VRS) var VRS = {};
if(!VRS.WebAdmin) VRS.WebAdmin = {};
if(!VRS.WebAdmin.StratuxPluginOptions) VRS.WebAdmin.StratuxPluginOptions = {};

VRS.WebAdmin.StratuxPluginOptions.PageHandler = function(viewId) {
    var self = this;
    self._ViewId = new VRS.WebAdmin.ViewId('StratuxPluginOptions', viewId);
    self._Model = null;
    self.refreshState();
};

VRS.WebAdmin.StratuxPluginOptions.PageHandler.prototype.showFailureMessage = function(message) {
    var alert = $('#failure-message');
    if(message && message.length) {
        alert.text(message || '').show();
    } else {
        alert.hide();
    }
};

VRS.WebAdmin.StratuxPluginOptions.PageHandler.prototype.refreshState = function() {
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

VRS.WebAdmin.StratuxPluginOptions.PageHandler.prototype.save = function() {
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

VRS.WebAdmin.StratuxPluginOptions.PageHandler.prototype.buildAjaxSettingsForSendConfiguration = function() {
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

VRS.WebAdmin.StratuxPluginOptions.PageHandler.prototype.applyState = function(state) {
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
                    }
                }
            });
            ko.applyBindings(self._Model);
        }
    }
};
