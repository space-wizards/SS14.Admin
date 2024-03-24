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

    for (let i = 0; i < cookies.length; i++) {
        let cookie = cookies[i].trim();
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

function toggleDescription(id) {
    var description = document.getElementById('description-' + id);
    if (description.style.height === '0px') {
        description.style.height = description.scrollHeight + 'px';
    } else {
        description.style.height = '0px';
    }
}

