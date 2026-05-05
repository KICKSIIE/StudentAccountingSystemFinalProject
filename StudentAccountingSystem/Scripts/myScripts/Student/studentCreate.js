$(function () {
    var currentYear = new Date().getFullYear();
    var opts = "";
    for (var i = -1; i <= 3; i++) {
        var sy = (currentYear + i) + "-" + (currentYear + i + 1);
        var selected = i === 0 ? "selected" : "";
        opts += "<option value='" + sy + "' " + selected + ">" + sy + "</option>";
    }
    $("#selSchoolYear").html(opts);
});

$("#btnRegister").click(function () {
    $("#msgBox").html("");

    if (!$("#txtLastName").val().trim()) {
        $("#msgBox").html("<span class='msg-error'>Last Name is required.</span>");
        return;
    }
    if (!$("#txtFirstName").val().trim()) {
        $("#msgBox").html("<span class='msg-error'>First Name is required.</span>");
        return;
    }
    if (!$("#selCourse").val()) {
        $("#msgBox").html("<span class='msg-error'>Please select a Course.</span>");
        return;
    }
    if (!$("#txtBirthDate").val()) {
        $("#msgBox").html("<span class='msg-error'>Birth Date is required.</span>");
        return;
    }

    $(this).prop("disabled", true).text("Registering...");

    $.post("/Student/RegisterStudent", {
        lastName: $("#txtLastName").val(),
        firstName: $("#txtFirstName").val(),
        middleName: $("#txtMiddleName").val(),
        gender: $("#selGender").val(),
        birthDate: $("#txtBirthDate").val(),
        contact: $("#txtContact").val(),
        address: $("#txtAddress").val(),
        email: $("#txtEmail").val(),
        courseID: $("#selCourse").val(),
        yearLevel: $("#selYear").val(),
        semester: $("#selSemester").val(),
        schoolYear: $("#selSchoolYear").val(),
        scholarshipID: $("#selScholarship").val(),
        discountID: $("#selDiscount").val()
    }, function (res) {
        if (res[0].mess == 0) {
            $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
        } else {
            $("#msgBox").html("<span class='msg-success'>Student registered. Student No: " + res[0].studentNumber + "</span>");
            $("#txtLastName, #txtFirstName, #txtMiddleName, #txtBirthDate, #txtContact, #txtAddress, #txtEmail").val("");
            $("#selGender, #selCourse, #selScholarship, #selDiscount").val("");
            $("#selYear").val("1");
            $("#selSemester").val("1st");
        }
    }).fail(function (xhr) {
        $("#msgBox").html("<span class='msg-error'>Error: " + xhr.responseText + "</span>");
    }).always(function () {
        $("#btnRegister").prop("disabled", false).text("Register Student");
    });
});
