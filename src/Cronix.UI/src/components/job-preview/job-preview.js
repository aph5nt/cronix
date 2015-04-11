define(['knockout', 'text!./job-preview.html'], function(ko, templateMarkup) {

  function JobPreview(params) {
    this.message = ko.observable('Hello from the job-preview component!');
  }

  // This runs when the component is torn down. Put here any logic necessary to clean up,
  // for example cancelling setTimeouts or disposing Knockout subscriptions/computeds.
  JobPreview.prototype.dispose = function() { };
  
  return { viewModel: JobPreview, template: templateMarkup };

});
