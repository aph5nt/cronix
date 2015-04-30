define(['knockout', 'moment', 'signalr', 'text!./job-preview.html'], function (ko, moment, signalr, templateMarkup) {

    var connection = $.hubConnection();
    connection.url = "http://localhost:8111/signalr";

    var hub = connection.createHubProxy('ScheduleManager');

    var triggerStateCase = {
        Stopped: "Stopped",
        Idle: "Idle",
        Executing: "Executing",
        Terminated: "Terminated",
        Removed: "Removed"
    };

    function formatDate(date) {
        return moment(date).format('YYYY-MM-DD HH:mm');
    }

    var jobPreviewItem = function() {
        var self = this;

        self.commandToggled = ko.observable(false);
        self.cronExpr = ko.observable();
        self.nextOccuranceDate = ko.observable();
        self.triggerState = ko.observable(triggerStateCase.Idle);

        self.enableTrigger = function () {
            hub.invoke('enableTrigger', self.name);
            self.commandToggled(true);
        };

        self.disableTrigger = function () {
            hub.invoke('disableTrigger', self.name);
            self.commandToggled(true);
        };

        self.fireTrigger = function () {
            hub.invoke('fireTrigger', self.name);
            self.commandToggled(true);
        };

        self.terminateTrigger = function () {
            hub.invoke('terminateTrigger', self.name);
            self.commandToggled(true);
        };

        self.canEnableTrigger = ko.computed(function () {
            return self.triggerState() === triggerStateCase.Stopped && self.commandToggled() === false;
        });

        self.canDisableTrigger = ko.computed(function () {
            return self.triggerState() !== triggerStateCase.Stopped && self.commandToggled() === false;
        });
        
        self.canFireTrigger = ko.computed(function () {
            return self.triggerState() === triggerStateCase.Idle && self.commandToggled() === false;
        });

        self.canTerminateTrigger = ko.computed(function () {
            return self.triggerState() === triggerStateCase.Executing && self.commandToggled() === false;
        });
    };

    function jobPreview(params) {
        var self = this;

        self.items = ko.observableArray();

        hub.on('onStateChanged', function(jobState) {
            // update existing
            var existing = ko.utils.arrayFirst(self.items(), function(item) {
                return item.name === jobState.Name;
            });

            if (existing !== null) {
                if (existing.triggerState() === triggerStateCase.Removed) {
                    self.items.remove(existing);

                } else {
                    existing.cronExpr(jobState.CronExpr);
                    existing.triggerState(jobState.TriggerState.Case);
                    existing.nextOccuranceDate(formatDate(jobState.NextOccuranceDate));
                    existing.commandToggled(false);
                }
            } else {

                // add new item
                var item = new jobPreviewItem();
                item.name = jobState.Name;
                item.cronExpr(jobState.CronExpr);
                item.triggerState(jobState.TriggerState.Case);
                item.nextOccuranceDate(formatDate(jobState.NextOccuranceDate));
                item.commandToggled(false);

                self.items.push(item);
            }
        });

        hub.on('getData', function (input) {
            var items = [];

            input.forEach(function(jobState) {
                var item = new jobPreviewItem();
                item.name = jobState.Name;
                item.cronExpr(jobState.CronExpr);
                item.triggerState(jobState.TriggerState.Case);
                item.nextOccuranceDate(formatDate(jobState.NextOccuranceDate));
                item.commandToggled(false);
                items.push(item);
            });

            self.items(items);
        });

        connection.start().done(function () {
            hub.invoke('getData');
        });
    }

    return { viewModel: jobPreview, template: templateMarkup };

});