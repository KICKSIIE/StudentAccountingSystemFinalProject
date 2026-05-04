$("#btnLogin").click(function () {
    var username = $("#txtUsername").val();
    var password = $("#txtPassword").val();

    // Indicate loading state
    $("#btnLogin").val("Authenticating...").prop("disabled", true);

    $.post("../Account/DoLogin", {
        username: username,
        password: password
    }, function (res) {
        if (res[0].success) {
            window.location.href = res[0].redirect;
        } else {
            $("#result").html(res[0].message);
            $("#btnLogin").val("Login").prop("disabled", false);
        }
    });
});

// Allow pressing "Enter" to login
$(document).keypress(function (e) {
    if (e.which == 13) {
        $("#btnLogin").click();
    }
});
