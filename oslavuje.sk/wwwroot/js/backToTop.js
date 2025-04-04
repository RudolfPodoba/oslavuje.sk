// Back to top button functionality
$(document).ready(function () {
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('#backToTopButton').fadeIn();
        } else {
            $('#backToTopButton').fadeOut();
        }
    });

    $('#backToTopButton').click(function (event) {
        event.preventDefault();
        $('html, body').animate({ scrollTop: 0 }, 200); // Trvania v milisekundách. Teraz je 200.
        return false;
    });
});