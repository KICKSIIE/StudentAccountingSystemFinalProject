var allStudents = [];
var currentStudentID = 0;
var currentStudentName = "";
var currentAssessmentID = 0;
var currentBalance = 0;
var currentPeriod = "";

$(function () {
    loadStudents();

    $("#btnRefresh").on("click", function () {
        loadStudents();
    });

    $("#searchStudent").on("keyup", function () {
        filterStudents();
    });

    $("#filterYear").on("change", function () {
        filterStudents();
    });

    $("#btnClosePayment").on("click", function () {
        $("#paymentPanel").hide();
    });

    $("#btnCancelPay").on("click", function () {
        $("#payModal").hide();
    });

    $("#btnConfirmPay").on("click", function () {
        confirmPayment();
    });
});

function loadStudents() {
    $("#studentTbody").html('<tr><td colspan="7" class="td-empty">Loading...</td></tr>');

    $.post(AppUrls.getStudents, {}, function (res) {
        if (res && res.length > 0) {
            allStudents = res;
            renderStudents(allStudents);
        } else {
            $("#studentTbody").html('<tr><td colspan="7" class="td-empty">No students found.</td></tr>');
        }
    }).fail(function () {
        $("#studentTbody").html('<tr><td colspan="7" class="td-empty msg-error">Error loading students.</td></tr>');
    });
}

function filterStudents() {
    var search = $("#searchStudent").val().toLowerCase();
    var yearFilter = $("#filterYear").val();

    var filtered = allStudents.filter(function (s) {
        var studentID = s.StudentID || s.studentID || "";
        var studentName = s.StudentName || s.studentName || "";
        var yearLevel = s.YearLevel || s.yearLevel || "";

        var matchSearch = search === "" ||
            studentID.toString().includes(search) ||
            studentName.toLowerCase().includes(search);

        var matchYear = yearFilter === "" || yearLevel.toString() === yearFilter;

        return matchSearch && matchYear;
    });

    renderStudents(filtered);
}

function renderStudents(students) {
    var tbody = $("#studentTbody");
    tbody.empty();

    if (students.length === 0) {
        tbody.append('<tr><td colspan="7" class="td-empty">No students match.</td></tr>');
        return;
    }

    students.forEach(function (s) {
        var studentID = s.StudentID || s.studentID || "?";
        var studentName = s.StudentName || s.studentName || "Unknown";
        var courseCode = s.CourseCode || s.courseCode || "?";
        var yearLevel = s.YearLevel || s.yearLevel || "?";
        var semester = s.Semester || s.semester || "?";
        var schoolYear = s.SchoolYear || s.schoolYear || "?";
        var safeName = studentName.replace(/'/g, "\\'");

        tbody.append(
            '<tr>' +
            '<td>' + studentID + '</td>' +
            '<td>' + studentName + '</td>' +
            '<td>' + courseCode + '</td>' +
            '<td>' + yearLevel + '</td>' +
            '<td>' + semester + '</td>' +
            '<td>' + schoolYear + '</td>' +
            '<td class="td-center">' +
            '<button class="btn-mini btn-select" onclick="selectStudent(' + studentID + ', \'' + safeName + '\')">Select</button>' +
            '</td>' +
            '</tr>'
        );
    });
}

function selectStudent(studentID, studentName) {
    currentStudentID = studentID;
    currentStudentName = studentName;
    $("#paymentTitle").text("Assessments — " + studentName + " (ID: " + studentID + ")");
    $("#paymentPanel").show();
    $("html,body").animate({ scrollTop: $("#paymentPanel").offset().top - 20 }, 300);
    loadAssessments(studentID);
}

function loadAssessments(studentID) {
    $("#assessmentTbody").html('<tr><td colspan="11" class="td-empty">Loading assessments...</td></tr>');

    $.post(AppUrls.getAssessments, { studentID: studentID }, function (res) {
        if (!res || res.length === 0) {
            $("#assessmentTbody").html('<tr><td colspan="11" class="td-empty">No assessments found. Generate them in Assessment Management first.</td></tr>');
            return;
        }

        res.sort(function (a, b) {
            if (a.YearLevel !== b.YearLevel) return a.YearLevel - b.YearLevel;
            var semA = a.Semester === "1st" ? 1 : 2;
            var semB = b.Semester === "1st" ? 1 : 2;
            if (semA !== semB) return semA - semB;
            var isRefA = a.Status === "Refunded" ? 0 : 1;
            var isRefB = b.Status === "Refunded" ? 0 : 1;
            if (isRefA !== isRefB) return isRefA - isRefB;
            var order = { "Prelim": 1, "Midterm": 2, "Semi-Final": 3, "Final": 4 };
            return order[a.Period] - order[b.Period];
        });

        var tbody = $("#assessmentTbody").empty();

        res.forEach(function (a) {
            var amountDue = a.AmountDue || 0;
            var totalPaid = a.TotalPaid || 0;
            var balance = Math.max((a.Balance || (amountDue - totalPaid)), 0);
            var status = a.Status || "Unknown";
            var paymentIDs = a.PaymentIDs || "—";

            var badgeClass = {
                "Paid": "badge badge-paid",
                "Unpaid": "badge badge-unpaid",
                "Partial": "badge badge-partial",
                "Locked": "badge badge-locked",
                "Refunded": "badge badge-refunded"
            }[status] || "badge";

            var actionBtn = "";
            if (status === "Unpaid" || status === "Partial") {
                actionBtn = '<button class="btn-mini btn-pay" onclick="openPayModal(' + a.AssessmentID + ', \'' + a.Period + '\', ' + balance + ')">Pay</button>';
            } else if (status === "Locked") {
                actionBtn = '<span style="color:#94a3b8; font-size:0.8rem;">Locked</span>';
            } else if (status === "Refunded") {
                actionBtn = '<span style="color:#94a3b8; font-size:0.8rem;">—</span>';
            }

            tbody.append(
                '<tr>' +
                '<td>' + a.AssessmentID + '</td>' +
                '<td>' + a.YearLevel + '</td>' +
                '<td>' + a.Semester + '</td>' +
                '<td>' + a.Period + '</td>' +
                '<td>' + a.SchoolYear + '</td>' +
                '<td>PHP ' + fmt(amountDue) + '</td>' +
                '<td>PHP ' + fmt(totalPaid) + '</td>' +
                '<td>PHP ' + fmt(balance) + '</td>' +
                '<td><span class="' + badgeClass + '">' + status + '</span></td>' +
                '<td>' + paymentIDs + '</td>' +
                '<td class="td-center">' + actionBtn + '</td>' +
                '</tr>'
            );
        });
    }).fail(function () {
        $("#assessmentTbody").html('<tr><td colspan="11" class="td-empty msg-error">Error loading assessments.</td></tr>');
    });
}

function openPayModal(assessmentID, period, balance) {
    currentAssessmentID = assessmentID;
    currentBalance = balance;
    currentPeriod = period;

    $("#payDesc").text(period + " — " + currentStudentName);
    $("#payBalance").text(fmt(balance));
    $("#payAmount").val(balance);
    $("#payModal").css("display", "flex");
    $("#payAmount").select();
}

function confirmPayment() {
    var amount = parseFloat($("#payAmount").val());

    if (isNaN(amount) || amount <= 0) {
        alert("Please enter a valid amount greater than zero.");
        return;
    }

    if (currentBalance === 0) {
        alert("This period has zero balance. No payment is needed.");
        $("#payModal").hide();
        loadAssessments(currentStudentID);
        return;
    }

    if (amount > currentBalance) {
        alert("Amount cannot exceed the balance of PHP " + fmt(currentBalance) + ".");
        return;
    }

    if (amount < currentBalance) {
        if (!confirm("You are paying PHP " + fmt(amount) + ", which is less than the balance of PHP " + fmt(currentBalance) + ". This will be recorded as a partial payment. Continue?")) {
            return;
        }
    }

    var $btn = $("#btnConfirmPay").prop("disabled", true).text("Processing...");

    $.post(AppUrls.recordPayment, {
        assessmentID: currentAssessmentID,
        studentID: currentStudentID,
        amountPaid: amount,
        remarks: ""
    }, function (res) {
        $("#payModal").hide();
        $btn.prop("disabled", false).text("Record Payment");

        if (res.mess === 1) {
            $("#msgBox").html("<span class='msg-success'>" + res.message + "</span>");
            if (res.isGraduated === true) {
                alert("The student has completed all requirements and is now marked as Graduated.");
                loadStudents();
            }
            loadAssessments(currentStudentID);
        } else {
            $("#msgBox").html("<span class='msg-error'>" + res.message + "</span>");
        }
    }, "json").fail(function () {
        $btn.prop("disabled", false).text("Record Payment");
        $("#msgBox").html("<span class='msg-error'>An error occurred while processing the payment. Please try again.</span>");
    });
}

function fmt(n) {
    return Number(n || 0).toLocaleString("en-PH", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}
