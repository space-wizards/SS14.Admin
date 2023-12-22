//Makes the calender dropdown
const datePicker = new DatePicker('datePicker', 'litepicker-close');

//String search
$(document).ready(function() {
    let x = $('#search');
    if (context.parameters.has("search")) x.val(context.parameters.get("search"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("search",currentValue);
    });
});

//Round
$(document).ready(function() {
    let x = $('#roundId');
    if (context.parameters.has("roundId")) x.val(context.parameters.get("roundId"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("roundId",currentValue);
    });
});

//Player
$(document).ready(function() {
    let x = $('#player');
    if (context.parameters.has("player")) x.val(context.parameters.get("player"));
    x.change(function() {
        var input = $(this);
        var currentValue = input.val();
        context.updateQuery("player",currentValue);
    });
});

//Logtype handled by the log-type-autocomplete.js

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

//Search key combination (removed for now to fix #10677)
// document.onkeydown = function (e) {
//     if (e.ctrlKey && e.key === 'f') {
//         e.preventDefault();
//         search.focus();
//     }
// };
