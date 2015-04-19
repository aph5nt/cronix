define(['knockout', 'signalr','text!./job-preview.html'], function(ko, signalr, templateMarkup) {

    var connection = $.hubConnection();
    connection.url = "http://localhost:8111/signalr";

    var hub = connection.createHubProxy('ScheduleManager');

    var triggerStateCase = {
        Stopped: "Stopped",
        Idle: "Idle",
        Executing: "Executing",
        Terminated: "Terminated"
    };

    var jobPreviewItem = function() {
        var self = this;

        self.commandToggled = ko.observable(false);
        self.cronExpr = ko.observable();
        self.triggerState = ko.observable(triggerStateCase.Idle);
        self.start = function() {
            hub.invoke('startJob', self.name);
            self.commandToggled(true);
        };
        self.stop = function() {
            hub.invoke('stopJob', self.name);
            self.commandToggled(true);
        };
        self.canStart = ko.computed(function() {
            return self.triggerState() !== triggerStateCase.Executing && self.commandToggled() === false;
        });
        self.canStop = ko.computed(function () {
            return self.triggerState() === triggerStateCase.Executing && self.commandToggled() === false;
        });

        
    };

    function jobPreview(params) {
        var self = this;

        self.items = ko.observableArray();
        
        hub.on('getData', function (input) {
            var items = [];

            input.forEach(function(jobState) {
                var item = new jobPreviewItem();
                item.name = jobState.Name;
                item.cronExpr(jobState.CronExpr);
                item.triggerState(jobState.TriggerState.Case);

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

/*

- cronix web --> cronix startup
- implement on state change + signalr

- durring the build Cronix.UI -> dist -> copy to cronix webui folder
- fake build, nugetpackage
- manual how to enable webui (just copy catalog)

 */