$("#btnUpdate").click(function () {
    $.post("/Course/UpdateCourse", {
        courseID: $("#hdnCourseID").val(),
        code: $("#txtCode").val(),
        name: $("#txtName").val(),
        units: $("#txtUnits").val(),
        tpu: $("#txtTpu").val(),
        reg: $("#txtReg").val(),
        misc: $("#txtMisc").val(),
        lab: $("#txtLab").val()
    }, function (res) {
        if (res.message) {
            $("#result").html("<p class='msg-success'>" + res.message + " Redirecting...</p>");
            setTimeout(function () {
                window.location.href = "/Course/Index";
            }, 2000);
        } else {
            $("#result").html("<p class='msg-error'>Error updating course.</p>");
        }
    }).fail(function () {
        $("#result").html("<p class='msg-error'>Request failed. Please try again.</p>");
    });
});