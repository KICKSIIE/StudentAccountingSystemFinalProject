$(document).ready(function () {
    loadStudents();

    $("#btnRefresh").click(function () {
        loadStudents();
    });

    $("#searchStudent").on("keyup", function () {
        filterStudents();
    });

    $("#filterStatus").on("change", function () {
        filterStudents();
    });

    $("#btnCloseDetail").click(function () {
        $("#detailPanel").hide();
    });
});

var allStudents = [];
var selectedStudentID = 0;

function loadStudents() {
    $("#studentTbody").html('<tr><td colspan="8">Loading...</td></td>');

    $.post("/Assessment/GetStudentAssessmentStatus", {}, function (res) {
        console.log("Response from server:", res); // DEBUG: Check what server returns

        if (res && res.length > 0) {
            allStudents = res;
            renderStudents(allStudents);
        } else {
            $("#studentTbody").html('<tr><td colspan="8">No students found.</td></td>');
        }
    }).fail(function (xhr, status, error) {
        console.log("AJAX Error:", error);
        $("#studentTbody").html('<tr><td colspan="8">Error loading students. Check console.</td></td>');
    });
}

function filterStudents() {
    var search = $("#searchStudent").val().toLowerCase();
    var filter = $("#filterStatus").val();

    var filtered = allStudents.filter(function (s) {
        // Handle possible undefined values
        var studentID = s.StudentID || s.studentID || "";
        var studentName = s.StudentName || s.studentName || "";

        var matchSearch = search === "" ||
            studentID.toString().includes(search) ||
            studentName.toLowerCase().includes(search);

        var matchFilter = true;
        if (filter === "can_generate") {
            matchFilter = (s.CanGenerate === true || s.canGenerate === true);
        } else if (filter === "next_ready") {
            matchFilter = (s.NextCanGenerate === true || s.nextCanGenerate === true) &&
                !(s.IsGraduating === true || s.isGraduating === true);
        }

        return matchSearch && matchFilter;
    });

    renderStudents(filtered);
}

function renderStudents(students) {
    var tbody = $("#studentTbody");
    tbody.empty();

    if (students.length === 0) {
        tbody.append('<tr><td colspan="8">No students match.</td></tr>');
        return;
    }

    for (var i = 0; i < students.length; i++) {
        var s = students[i];

        // Get values with fallbacks for both capitalized and lowercase property names
        var studentID = s.StudentID || s.studentID || "?";
        var studentName = s.StudentName || s.studentName || "Unknown";
        var courseCode = s.CourseCode || s.courseCode || "?";
        var yearLevel = s.YearLevel || s.yearLevel || "?";
        var semester = s.Semester || s.semester || "?";
        var schoolYear = s.SchoolYear || s.schoolYear || "?";
        var canGenerate = s.CanGenerate || s.canGenerate || false;
        var nextCanGenerate = s.NextCanGenerate || s.nextCanGenerate || false;
        var isGraduating = s.IsGraduating || s.isGraduating || false;
        var nextSem = s.NextSem || s.nextSem || "";
        var nextYear = s.NextYear || s.nextYear || "";
        var nextSY = s.NextSY || s.nextSY || "";

        var currentBtn = "";
        if (canGenerate) {
            currentBtn = '<button class="btn-gen" onclick="confirmGenerate(' + studentID + ', false)">Generate</button>';
        } else {
            currentBtn = '<span class="status-paid">Generated</span>';
        }

        var nextBtn = "";
        if (isGraduating) {
            nextBtn = '<span style="color:gray;">Graduated</span>';
        } else if (nextCanGenerate) {
            nextBtn = '<button class="btn-next" onclick="confirmGenerate(' + studentID + ', true)">Generate Next</button>';
        } else {
            nextBtn = '<span class="status-locked">Locked</span>';
        }

        var row = '<tr class="student-row" onclick="loadStudentDetail(' + studentID + ', \'' + studentName.replace(/'/g, "\\'") + '\')">' +
            '<td>' + studentID + '</td>' +
            '<td>' + studentName + '</td>' +
            '<td>' + courseCode + '</td>' +
            '<td>' + yearLevel + '</td>' +
            '<td>' + semester + '</td>' +
            '<td>' + schoolYear + '</td>' +
            '<td>' + currentBtn + '</td>' +
            '<td>' + nextBtn + '</td>' +
            '</tr>';

        tbody.append(row);
    }
}

function confirmGenerate(studentID, isNext) {
    var student = null;
    for (var i = 0; i < allStudents.length; i++) {
        var s = allStudents[i];
        var sid = s.StudentID || s.studentID;
        if (sid === studentID) {
            student = s;
            break;
        }
    }

    if (!student) {
        alert("Student not found.");
        return;
    }

    // Get values with fallbacks
    var studentName = student.StudentName || student.studentName || "Student";
    var sem = isNext ? (student.NextSem || student.nextSem) : (student.Semester || student.semester);
    var sy = isNext ? (student.NextSY || student.nextSY) : (student.SchoolYear || student.schoolYear);
    var yr = isNext ? (student.NextYear || student.nextYear) : (student.YearLevel || student.yearLevel);

    var msg = "Generate assessments for:\n";
    msg += "Student: " + studentName + " (ID: " + studentID + ")\n";
    msg += "Year Level: " + yr + "\n";
    msg += "Semester: " + sem + "\n";
    msg += "School Year: " + sy + "\n\n";
    msg += "This will create 4 periods: Prelim, Midterm, Semi-Final, Final.";

    if (confirm(msg)) {
        var url = isNext ? "/Assessment/GenerateNextSemester" : "/Assessment/GenerateAssessment";
        $.post(url, { studentID: studentID }, function (res) {
            if (res.mess === 1) {
                alert("Success: " + res.message);
                loadStudents();
                if (selectedStudentID === studentID) {
                    loadStudentDetail(studentID, studentName);
                }
            } else {
                alert("Error: " + res.message);
            }
        }, "json");
    }
}

function loadStudentDetail(studentID, studentName) {
    selectedStudentID = studentID;
    $("#detailTitle").text("Assessments - " + studentName + " (ID: " + studentID + ")");
    $("#detailPanel").show();
    $("#assessmentTbody").html('<tr><td colspan="10">Loading assessments...</td></tr>');

    $.post("/Assessment/GetStudentAssessments", { studentID: studentID }, function (res) {
        console.log("Assessments response:", res);

        var tbody = $("#assessmentTbody");
        tbody.empty();

        if (!res || res.length === 0) {
            tbody.append('<tr><td colspan="10">No assessments generated yet. Generate them first.</td></tr>');
            return;
        }

        for (var i = 0; i < res.length; i++) {
            var a = res[i];

            var assessmentID = a.AssessmentID || a.assessmentID;
            var period = a.Period || a.period;
            var yearLevel = a.YearLevel || a.yearLevel;
            var semester = a.Semester || a.semester;
            var schoolYear = a.SchoolYear || a.schoolYear;
            var amountDue = a.AmountDue || a.amountDue || 0;
            var totalPaid = a.TotalPaid || a.totalPaid || 0;
            var balance = a.Balance || a.balance || (amountDue - totalPaid);
            var status = a.Status || a.status || "Unknown";

            var statusClass = "";
            if (status === "Paid") statusClass = "status-paid";
            else if (status === "Unpaid") statusClass = "status-unpaid";
            else if (status === "Locked") statusClass = "status-locked";
            else if (status === "Partial") statusClass = "status-partial";
            else if (status === "Refunded") statusClass = "status-refunded";

            var statusHtml = '<span class="' + statusClass + '">' + status + '</span>';

            var actionHtml = "";
            if (status === "Locked") {
                actionHtml = '<button class="btn-unlock" onclick="unlockPeriod(' + assessmentID + ')">Unlock</button>';
            }

            var row = '<tr>' +
                '<td>' + assessmentID + '</td>' +      // NEW: Assessment ID column
                '<td>' + yearLevel + '</td>' +
                '<td>' + semester + '</td>' +
                '<td>' + period + '</td>' +
                '<td>' + schoolYear + '</td>' +
                '<td>₱' + formatNumber(amountDue) + '</td>' +
                '<td>₱' + formatNumber(totalPaid) + '</td>' +
                '<td>₱' + formatNumber(balance) + '</td>' +
                '<td>' + statusHtml + '</td>' +
                '<td>' + actionHtml + '</td>' +
                '</tr>';

            tbody.append(row);
        }
    }, "json");
}

function unlockPeriod(assessmentID) {
    if (confirm("Unlock this period? Student can then pay for it.")) {
        $.post("/Assessment/Unlock", { id: assessmentID }, function (res) {
            if (res.mess === 1) {
                alert("Unlocked successfully.");
                if (selectedStudentID) {
                    // Need to reload student detail - get student name from somewhere
                    loadStudentDetail(selectedStudentID, "");
                }
            } else {
                alert("Error: " + res.message);
            }
        }, "json");
    }
}

function formatNumber(num) {
    if (num === undefined || num === null) return "0.00";
    return parseFloat(num).toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}