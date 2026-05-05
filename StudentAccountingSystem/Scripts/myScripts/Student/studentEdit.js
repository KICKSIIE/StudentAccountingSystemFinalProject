$("#btnUpdate").click(function () {
    $.post("/Student/UpdateStudent", {
        studentID: $("#hdnStudentID").val(),
        lastName: $("#txtLname").val(),
        firstName: $("#txtFname").val(),
        middleName: $("#txtMname").val(),
        gender: $("#selGender").val(),
        birthDate: $("#txtBirthDate").val(),
        contact: $("#txtContact").val(),
        address: $("#txtAddress").val(),
        email: $("#txtEmail").val(),
        courseID: $("#selCourse").val(),
        yearLevel: $("#selYearLevel").val(),
        semester: $("#selSemester").val(),
        schoolYear: $("#selSchoolYear").val(),
        status: $("#selStatus").val(),
        scholarshipID: $("#selScholarship").val(),
        discountID: $("#selDiscount").val()
    }, function (res) {
        if (res[0].mess == 1) {
            $("#result").html("<p class='msg-success'>" + res[0].message + " Redirecting...</p>");
            setTimeout(function () {
                window.location.href = "/Student/Index";
            }, 2000);
        } else {
            $("#result").html("<p class='msg-error'>" + res[0].message + "</p>");
        }
    }).fail(function () {
        $("#result").html("<p class='msg-error'>Request failed. Please try again.</p>");
    });
});