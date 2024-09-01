//Makes the calender dropdown
const datePicker = new DatePicker('datePicker', 'litepicker-close');

//String search
$(document).ready(function() {
    let x = $('#messageSearch');
    if (context.parameters.has("messageSearch")) x.val(context.parameters.get("messageSearch"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("messageSearch",currentValue);
    });
});

// Author
$(document).ready(function() {
    let x = $('#author');
    if (context.parameters.has("author")) x.val(context.parameters.get("author"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("author",currentValue);
    });
});

$(document).ready(function() {
    let x = $('#effected');
    if (context.parameters.has("effected")) x.val(context.parameters.get("effected"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("effected",currentValue);
    });
});

//Logtype handled by the audit-log-type-autocomplete.js

//Severity
$(document).ready(function() {
    let x = $('#severitySelect');
    if (context.parameters.has("severitySelect")) x.val(context.parameters.get("severity"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("severity",currentValue);
    });
});

//Pagination
$(document).ready(function() {
    let x = $('#countselect');
    if (context.parameters.has("countselect")) x.val(context.parameters.get("countselect"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("countselect",currentValue);
    });
});
