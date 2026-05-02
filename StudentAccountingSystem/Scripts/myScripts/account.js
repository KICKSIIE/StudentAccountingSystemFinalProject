$(document).ready(function () {

    // Disable button and show loading on submit
    $('form').on('submit', function () {
        var btn = $('#btnLogin');
        btn.prop('disabled', true).text('Logging in...');
    });

    // Re-enable if validation fails (page stays)
    window.onpageshow = function () {
        var btn = $('#btnLogin');
        btn.prop('disabled', false).text('Login');
    };

});