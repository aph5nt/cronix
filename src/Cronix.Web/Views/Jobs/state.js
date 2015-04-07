window.cronix = window.cronix || {};

$(document).ready(function () {
    
    cronix.stateItem = {
        name: ko.observable(''),
        cronExpr: ko.observable(''),
        stateType: ko.observable(''),
        nextOccurance: ko.observable('')
    };

    cronix.StateViewModel = function() {
       
        var state = ko.observableArray([]),
        hasAny = ko.computed(function () {
            console.log(state.length);
            return state.length > 0;
        }),
        initialize = function() {
            $.connection.hub.url = "http://localhost:8080/signalr";
            var hub = $.connection.exchangeHub;
            hub.client.UpdateContent = function (data) {
                alert(data);
            };
            $.connection.hub.start().done(function () {
                hub.server.GetInitialState().done(function(state) {
                    console.log(state);
                });
            });
        };

        return {
            state: state,
            hasAny: hasAny,
            initialize: initialize
        };
    }

    var vm = new cronix.StateViewModel();
    vm.initialize();
    ko.applyBindings(vm);
   
});
 
 