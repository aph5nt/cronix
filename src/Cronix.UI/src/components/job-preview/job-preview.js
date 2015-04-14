define(['knockout', 'signalr','text!./job-preview.html'], function(ko, signalr, templateMarkup) {


    var triggerStateType = {
        Stopped: { name: "Stopped", value: 0 },
        Idle: { name: "Idle", value: 1 },
        Executing: { name: "Executing", value: 2 },
        Terminated: { name: "Terminated", value: 3 },
    };

    var jobPreviewItem = function() {
        var self = this;
        var name = "",
            cronExpr = ko.observable(),
            triggerState = ko.observable(),
            start = function() {},
            stop = function() {},
            canStart = ko.computed(function() {}),
            canStop = ko.computed(function() {});

        return {
            name: name,
            cronExpr: cronExpr,
            triggerState: triggerState,
            start: start,
            stop: stop,
            canStart: canStart,
            canStop: canStop
        }
    };

    function jobPreview(params) {
        var self = this;

        self.items = ko.observableArray();
        self.load = function () {

            var connection = $.hubConnection();
            connection.url = "http://localhost:8111/signalr";

            var hub = connection.createHubProxy('SampleHub');

            hub.on('getData', function (input) {
                var i = new jobPreviewItem();
                i.name = "My Job";
                i.cronExpr = "* * * * *";
                i.triggerState = triggerStateType.Idle.name;
                self.items([i]);
            });
 

            connection.start().done(function () {
                hub.invoke('getData', 'duppaaaaa');
            });
        }

        self.load();
    }

    return { viewModel: jobPreview, template: templateMarkup };

});