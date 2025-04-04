// Presmerovanie z HTTP na HTTPS
(function () {
    if (window.location.protocol != 'https:' && window.location.hostname !== 'localhost') {
        location.href = location.href.replace("http://", "https://");
    }
})();