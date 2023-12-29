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

function toggleDarkMode() {
    let darkModeCookie = getCookie("darkMode");
    if (darkModeCookie === "" || darkModeCookie === "false") {
        document.body.classList.add("dark-mode");
        setCookie("darkMode", "true", 365);
    } else {
        document.body.classList.remove("dark-mode");
        setCookie("darkMode", "false", 365);
    }

    location.reload();
}

function getCookie(name) {
    let cookieName = name + "=";
    let cookies = document.cookie.split(';');

    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i].trim();
        if (cookie.indexOf(cookieName) === 0) {
            return cookie.substring(cookieName.length, cookie.length);
        }
    }

    return "";
}

function setCookie(name, value, days) {
    let expirationDate = new Date();
    expirationDate.setDate(expirationDate.getDate() + days);
    let cookieValue = name + "=" + value + ";expires=" + expirationDate.toUTCString() + ";path=/";
    document.cookie = cookieValue;
}
