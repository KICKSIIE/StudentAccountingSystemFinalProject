$("#btnLogin").click(function () {

    var username = $("#txtUsername").val();
    var password = $("#txtPassword").val();

    if (username == "" || password == "") {
        $("#result").html("<h3>Please enter username and password.</h3>");
        return;
    }

    $.post("../Account/DoLogin", {
        username: username,
        password: password
    }, function (res) {
        if (res.success) {
            window.location.href = res.redirect;
        } else {
            $("#result").html("<h3>" + res.message + "</h3>");
        }
    });

});