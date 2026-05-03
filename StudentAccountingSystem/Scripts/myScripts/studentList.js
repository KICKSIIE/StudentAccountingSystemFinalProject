$(document).ready(function () {
    loadCourses();
    loadScholarships();
    loadDiscounts();
    loadStudents();
    loadSchoolYears(); // New function to load school years
});

// New function to generate school years (last 5 years and next 5 years)
function loadSchoolYears() {
    var currentYear = new Date().getFullYear();
    var options = "";

    // Generate school years from 5 years ago to 5 years ahead
    for (var i = -5; i <= 5; i++) {
        var startYear = currentYear + i;
        var endYear = startYear + 1;
        var schoolYear = startYear + "-" + endYear;
        var selected = (i === 0) ? "selected" : ""; // Auto-select current year
        options += "<option value='" + schoolYear + "' " + selected + ">" + schoolYear + "</option>";
    }

    $("#selSchoolYear").html(options);
}

function loadStudents() {
    console.log("loadStudents called");

    $.post("../Student/GetAllStudents", {}, function (res) {
        console.log("Found " + res.length + " students");

        var tr = "";
        var i = 1;
        if (res.length == 0) {
            tr = "<tr><td colspan='10'>No students found.</td></tr>";
        } else {
            for (var rec in res) {
                tr += "<tr>";
                tr += "<td>" + i++ + "</td>";
                tr += "<td>" + (res[rec].idno || '') + "</td>";
                tr += "<td>" + (res[rec].lastname || '') + "</td>";
                tr += "<td>" + (res[rec].firstname || '') + "</td>";
                tr += "<td>" + (res[rec].course || '') + "</td>";
                tr += "<td>" + (res[rec].year || '') + "</td>";
                tr += "<td>" + (res[rec].semester || '') + "</td>";
                tr += "<td>" + (res[rec].schoolYear || '') + "</td>";
                tr += "<td>" + (res[rec].status || '') + "</td>";
                tr += "<td>";
                tr += "<input type='button' value='View' onclick='viewStudent(" + res[rec].tid + ")' /> ";
                tr += "<input type='button' value='Edit' onclick='showEdit(" + res[rec].tid + ")' /> ";
                tr += "<input type='button' value='Delete' onclick='deleteStudent(" + res[rec].tid + ")' />";
                tr += "</td>";
                tr += "</tr>";
            }
        }
        $("#tblBody").html(tr);
    }).fail(function () {
        alert("Failed to load students!");
        $("#tblBody").html("<tr><td colspan='10'>Error loading students</td></tr>");
    });
}

function loadCourses() {
    $.post("../Student/GetCourseList", {}, function (res) {
        var opts = "<option value=''>-- Select Course --</option>";
        for (var r in res) {
            opts += "<option value='" + res[r].courseID + "'>" +
                res[r].courseCode + " - " + res[r].courseName + "</option>";
        }
        $("#selCourse, #editCourse").html(opts);
    }).fail(function () {
        console.log("Error loading courses");
    });
}

function loadScholarships() {
    $.post("../Student/GetScholarshipList", {}, function (res) {
        var opts = "<option value=''>-- None --</option>";
        for (var r in res) {
            var val = res[r].type == "Percent"
                ? res[r].discountPercent + "%"
                : "P" + res[r].discountAmount;
            opts += "<option value='" + res[r].scholarshipID + "'>" +
                res[r].scholarshipName + " (" + val + ")</option>";
        }
        $("#selScholarship, #editScholarship").html(opts);
    }).fail(function () {
        console.log("Error loading scholarships");
    });
}

function loadDiscounts() {
    $.post("../Student/GetDiscountList", {}, function (res) {
        var opts = "<option value=''>-- None --</option>";
        for (var r in res) {
            var val = res[r].type == "Percent"
                ? res[r].discountPercent + "%"
                : "P" + res[r].discountAmount;
            opts += "<option value='" + res[r].discountID + "'>" +
                res[r].discountName + " (" + val + ")</option>";
        }
        $("#selDiscount, #editDiscount").html(opts);
    }).fail(function () {
        console.log("Error loading discounts");
    });
}

$("#btnShowRegister").click(function () {
    $("#divRegister").show();
    $("#divEdit").hide();
    $("#divList").hide();
});

$("#btnCancelRegister").click(function () {
    $("#divRegister").hide();
    $("#divList").show();
});

$("#btnCancelEdit").click(function () {
    $("#divEdit").hide();
    $("#divList").show();
});

$("#btnSearch").click(function () {
    var keyword = $("#txtSearch").val().toLowerCase();
    $("#tblBody tr").each(function () {
        $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
    });
});

$("#btnRegister").click(function () {
    // Validate required fields
    if (!$("#txtLastName").val()) {
        alert("Please enter Last Name");
        return;
    }
    if (!$("#txtFirstName").val()) {
        alert("Please enter First Name");
        return;
    }
    if (!$("#selCourse").val()) {
        alert("Please select a Course");
        return;
    }
    if (!$("#selYear").val()) {
        alert("Please select Year Level");
        return;
    }
    if (!$("#selSemester").val()) {
        alert("Please select Semester");
        return;
    }
    if (!$("#selSchoolYear").val()) {
        alert("Please select School Year");
        return;
    }

    console.log("Register button clicked - sending data");

    $.post("../Student/RegisterStudent", {
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
        console.log("Response received:", res);
        if (res[0].mess == 0) {
            $("#msgBox").html("<b style='color:red;'>" + res[0].message + "</b>");
            alert("Error: " + res[0].message);
        } else {
            $("#msgBox").html("<b style='color:green;'>Registered! Student No: " + res[0].studentNumber + "</b>");
            alert("Student registered successfully!");
            $("#divRegister").hide();
            $("#divList").show();
            loadStudents();
            // Clear form
            $("#txtLastName, #txtFirstName, #txtMiddleName, #txtBirthDate, #txtContact, #txtAddress, #txtEmail").val("");
            $("#selGender, #selCourse, #selYear, #selSemester, #selScholarship, #selDiscount").val("");
        }
    }).fail(function (xhr, status, error) {
        console.log("AJAX Error:", error);
        console.log("Status:", status);
        console.log("Response:", xhr.responseText);
        alert("Error registering student: " + error + "\nCheck console for details");
        $("#msgBox").html("<b style='color:red;'>Error: " + xhr.responseText + "</b>");
    });
});

function showEdit(studentID) {
    $.post("../Student/GetStudent", { studentID: studentID }, function (res) {
        if (res.length == 0) return;
        var s = res[0];
        $("#editStudentID").val(s.studentID);
        $("#editStudentNumber").text(s.studentNumber);
        $("#editLastName").val(s.lastName);
        $("#editFirstName").val(s.firstName);
        $("#editMiddleName").val(s.middleName);
        $("#editGender").val(s.gender);
        $("#editBirthDate").val(s.birthDate);
        $("#editContact").val(s.contact);
        $("#editAddress").val(s.address);
        $("#editEmail").val(s.email);
        $("#editCourse").val(s.courseID);
        $("#editYear").val(s.yearLevel);
        $("#editSemester").val(s.semester);
        $("#editSchoolYear").val(s.schoolYear);
        $("#editStatus").val(s.status);
        $("#editScholarship").val(s.scholarshipID);
        $("#editDiscount").val(s.discountID);
        $("#divList").hide();
        $("#divRegister").hide();
        $("#divEdit").show();
    });
}

$("#btnUpdate").click(function () {
    $.post("../Student/UpdateStudent", {
        studentID: $("#editStudentID").val(),
        lastName: $("#editLastName").val(),
        firstName: $("#editFirstName").val(),
        middleName: $("#editMiddleName").val(),
        gender: $("#editGender").val(),
        birthDate: $("#editBirthDate").val(),
        contact: $("#editContact").val(),
        address: $("#editAddress").val(),
        email: $("#editEmail").val(),
        courseID: $("#editCourse").val(),
        yearLevel: $("#editYear").val(),
        semester: $("#editSemester").val(),
        schoolYear: $("#editSchoolYear").val(),
        status: $("#editStatus").val(),
        scholarshipID: $("#editScholarship").val(),
        discountID: $("#editDiscount").val()
    }, function (res) {
        if (res[0].mess == 0) {
            $("#msgBox").html("<b style='color:red;'>" + res[0].message + "</b>");
        } else {
            $("#msgBox").html("<b style='color:green;'>" + res[0].message + "</b>");
            $("#divEdit").hide();
            $("#divList").show();
            loadStudents();
        }
    });
});

function deleteStudent(studentID) {
    if (!confirm("Delete this student? This cannot be undone.")) return;
    $.post("../Student/DeleteStudent", { studentID: studentID }, function (res) {
        if (res[0].mess == 0) {
            $("#msgBox").html("<b style='color:red;'>" + res[0].message + "</b>");
        } else {
            $("#msgBox").html("<b style='color:green;'>" + res[0].message + "</b>");
            loadStudents();
        }
    });
}

function viewStudent(studentID) {
    window.location.href = "/Student/Details/" + studentID;
}