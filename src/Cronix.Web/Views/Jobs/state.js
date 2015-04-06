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
        initialize = function() {};

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
 
 