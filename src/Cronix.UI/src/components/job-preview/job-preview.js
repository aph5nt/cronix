define(['knockout', 'text!./job-preview.html'], function(ko, templateMarkup) {


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
            // init signalr and fetch initial data and load them into items
            var i = new jobPreviewItem();
            i.name = "My Job";
            i.cronExpr = "* * * * *";
            i.triggerState = triggerStateType.Idle.name;
            self.items = [i];
        }
    }


    alert('ccc');

    return { viewModel: jobPreview, template: templateMarkup };

});