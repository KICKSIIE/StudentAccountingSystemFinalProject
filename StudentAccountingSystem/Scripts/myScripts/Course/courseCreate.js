$("#btnSave").click(function () {
    var data = {
        code: $("#txtCode").val().trim(),
        name: $("#txtName").val().trim(),
        units: $("#txtUnits").val(),
        tpu: $("#txtTpu").val(),
        reg: $("#txtReg").val(),
        misc: $("#txtMisc").val(),
        lab: $("#txtLab").val()
    };

    if (!data.code || !data.name) {
        $("#msgBox").html("<span class='msg-error'>Course Code and Name are required.</span>");
        return;
    }

    $.post("/Course/SaveCourse", data, function (res) {
        $("#msgBox").html("<span class='msg-success'>" + res.message + "</span>");
        setTimeout(function () {
            window.location.href = "/Course/Index";
        }, 1000);
    }).fail(function () {
        $("#msgBox").html("<span class='msg-error'>Error saving course. Please try again.</span>");
    });
});