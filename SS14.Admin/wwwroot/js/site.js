// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const DateTime = luxon.DateTime;

const context = {
    parameters: new URLSearchParams(window.location.search),
    update: () => location.assign('?' + context.parameters.toString()),
    updateQuery: function (key, value) {
        this.parameters.set(key, value);
        this.update();
    },
    parameterNames: {
        PAGE_INDEX: "pageIndex"
    }
}
