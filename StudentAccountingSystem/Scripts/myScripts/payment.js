$(document).ready(function () {
    loadStudents();

    $("#btnRefresh").click(function () {
        loadStudents();
    });

    $("#searchStudent").on("keyup", function () {
        filterStudents();
    });

    $("#filterYear").on("change", function () {
        filterStudents();
    });

    $("#btnClosePayment").click(function () {
        $("#paymentPanel").hide();
    });

    $("#btnCancelPay").click(function () {
        $("#payModal").hide();
    });
});

var allStudents = [];
var currentStudentID = 0;
var currentStudentName = "";
var currentAssessmentID = 0;
var currentBalance = 0;
var currentPeriod = "";

function loadStudents() {
    $("#studentTbody").html('<tr><td colspan="7">Loading...</td></tr>');

    $.post("/Payment/GetStudentsForPayment", {}, function (res) {
        console.log("Students response:", res);

        if (res && res.length > 0) {
            allStudents = res;
            renderStudents(allStudents);
        } else {
            $("#studentTbody").html('<tr><td colspan="7">No students found.</td></tr>');
        }
    }).fail(function (xhr, status, error) {
        console.log("AJAX Error:", error);
        $("#studentTbody").html('<tr><td colspan="7">Error loading students. Check console.</td></tr>');
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
        tbody.append('<tr><td colspan="7">No students match.</td></tr>');
        return;
    }

    for (var i = 0; i < students.length; i++) {
        var s = students[i];

        var studentID = s.StudentID || s.studentID || "?";
        var studentName = s.StudentName || s.studentName || "Unknown";
        var courseCode = s.CourseCode || s.courseCode || "?";
        var yearLevel = s.YearLevel || s.yearLevel || "?";
        var semester = s.Semester || s.semester || "?";
        var schoolYear = s.SchoolYear || s.schoolYear || "?";

        var row = '<tr class="student-row" onclick="selectStudent(' + studentID + ', \'' + studentName.replace(/'/g, "\\'") + '\')">' +
            '<td>' + studentID + '</td>' +
            '<td>' + studentName + '</td>' +
            '<td>' + courseCode + '</td>' +
            '<td>' + yearLevel + '</td>' +
            '<td>' + semester + '</td>' +
            '<td>' + schoolYear + '</td>' +
            '<td><button class="btn-pay" onclick="event.stopPropagation(); selectStudent(' + studentID + ', \'' + studentName.replace(/'/g, "\\'") + '\')">Select</button></td>' +
            '</tr>';

        tbody.append(row);
    }
}

function selectStudent(studentID, studentName) {
    currentStudentID = studentID;
    currentStudentName = studentName;
    $("#paymentTitle").text("Payment - " + studentName + " (ID: " + studentID + ")");
    $("#paymentPanel").show();
    $('html,body').animate({ scrollTop: $('#paymentPanel').offset().top - 20 }, 400);
    loadAssessments(studentID);
}

function loadAssessments(studentID) {
    $("#assessmentTbody").html('<tr><td colspan="11">Loading assessments......</td></tr>');

    $.post("/Payment/GetStudentAssessmentsForPayment", { studentID: studentID }, function (res) {
        console.log("Assessments response:", res);

        var tbody = $("#assessmentTbody");
        tbody.empty();

        if (!res || res.length === 0) {
            tbody.append('<tr><td colspan="11">No assessments found. Generate them in Assessment Management first.</td></tr>');
            return;
        }

        for (var i = 0; i < res.length; i++) {
            var a = res[i];

            var assessmentID = a.AssessmentID;           // INTEGER ID
            var period = a.Period;
            var yearLevel = a.YearLevel;
            var semester = a.Semester;
            var schoolYear = a.SchoolYear;
            var amountDue = a.AmountDue || 0;
            var totalPaid = a.TotalPaid || 0;
            var balance = a.Balance || (amountDue - totalPaid);
            if (balance < 0) balance = 0;
            var status = a.Status || "Unknown";
            var paymentIDs = a.PaymentIDs || "";         // Payment IDs (e.g., "101, 102" or "101 (OR#1001)")

            var statusClass = "";
            if (status === "Paid") statusClass = "status-paid";
            else if (status === "Unpaid") statusClass = "status-unpaid";
            else if (status === "Locked") statusClass = "status-locked";
            else if (status === "Partial") statusClass = "status-partial";

            var statusHtml = '<span class="' + statusClass + '">' + status + '</span>';

            // Payment ID display
            var paymentIdDisplay = "";
            if (paymentIDs !== "") {
                paymentIdDisplay = '<span>' + paymentIDs + '</span>';
            } else {
                paymentIdDisplay = '<span >—</span>';
            }

            var actionBtn = "";
            if (balance <= 0) {
                actionBtn = '<span class="status-paid">Fully Paid</span>';
            } else if (status === "Unpaid" || status === "Partial") {
                actionBtn = '<button class="btn-pay" onclick="openPayModal(' + assessmentID + ', \'' + period + '\', ' + balance + ')">Pay</button>';
            } else if (status === "Locked") {
                actionBtn = '<span style="color:gray;">Locked</span>';
            } else if (status === "Paid") {
                actionBtn = '<span class="status-paid">Fully Paid</span>';
            }

            var row = '<tr>' +
                '<td>' + assessmentID + '</td>' +                    // Assessment ID (INTEGER)
                '<td>' + yearLevel + '</td>' +                       // Year Level
                '<td>' + semester + '</td>' +                        // Semester
                '<td>' + period + '</td>' +                          // Period
                '<td>' + schoolYear + '</td>' +                      // School Year
                '<td>₱' + formatNumber(amountDue) + '</td>' +        // Amount Due
                '<td>₱' + formatNumber(totalPaid) + '</td>' +        // Total Paid
                '<td>₱' + formatNumber(balance) + '</td>' +          // Balance
                '<td>' + statusHtml + '</td>' +                      // Status
                '<td>' + paymentIdDisplay + '</td>' +                // Payment ID(s)
                '<td>' + actionBtn + '</td>' +                       // Action
                '</tr>';

            tbody.append(row);
        }
    }).fail(function (xhr, status, error) {
        console.log("Error loading assessments:", error);
        $("#assessmentTbody").html('<tr><td colspan="11">Error loading assessments.</td></tr>');
    });
}
function openPayModal(assessmentID, period, balance) {
    currentAssessmentID = assessmentID;
    currentBalance = balance;
    currentPeriod = period;

    $("#payDesc").text("Payment for: " + period + " - " + currentStudentName);
    $("#payBalance").text(formatNumber(balance));

    // AUTO-FILL the input with the exact balance
    $("#payAmount").val(balance);

    // Show the modal
    $("#payModal").show();

    // Optional: Highlight the input so cashier can change if needed
    $("#payAmount").select();
}


$("#btnConfirmPay").click(function () {
    var amount = parseFloat($("#payAmount").val());

    if (isNaN(amount)) {
        alert("Please enter a valid amount.");
        return;
    }

    // If balance is ZERO, cannot pay
    if (currentBalance === 0) {
        alert("This period has ZERO balance (100% discount).\n\nNo payment needed. The period is already considered fully paid.");
        $("#payModal").hide();
        // Reload assessments to refresh the status
        loadAssessments(currentStudentID);
        return;
    }

    // Normal payment validations
    if (amount <= 0) {
        alert("Please enter an amount greater than zero.");
        return;
    }

    // NO EXCESS ALLOWED
    if (amount > currentBalance) {
        alert("Amount cannot exceed the balance of ₱" + formatNumber(currentBalance) + ".\nPlease enter the exact balance or less.");
        return;
    }

    // Confirm if paying less than full balance (partial payment)
    if (amount < currentBalance) {
        if (!confirm("You are paying ₱" + formatNumber(amount) + " which is LESS than the balance of ₱" + formatNumber(currentBalance) + ".\n\nThis will be a PARTIAL payment. Continue?")) {
            return;
        }
    }

    $.post("/Payment/RecordPayment", {
        assessmentID: currentAssessmentID,
        studentID: currentStudentID,
        amountPaid: amount,
        remarks: ""
    }, function (res) {
        $("#payModal").hide();
        if (res.mess === 1) {
            alert("Success: " + res.message);
            loadAssessments(currentStudentID);
            if (res.finalPaid === true) {
                alert("Final period is paid. Go to Assessment Management to generate the next semester.");
            }
            if (res.isGraduated === true) {
                alert("🎓 CONGRATULATIONS! The student has GRADUATED! 🎓");
                loadStudents();
            }
        } else {
            alert("Error: " + res.message);
        }
    }, "json");
});
function loadAssessments(studentID) {
    $("#assessmentTbody").html('<tr><td colspan="11">Loading assessments...</td></tr>');

    $.post("/Payment/GetStudentAssessmentsForPayment", { studentID: studentID }, function (res) {
        console.log("Assessments response:", res);

        var tbody = $("#assessmentTbody");
        tbody.empty();

        if (!res || res.length === 0) {
            tbody.append('<tr><td colspan="11">No assessments found. Generate them in Assessment Management first.</td></tr>');
            return;
        }

        for (var i = 0; i < res.length; i++) {
            var a = res[i];

            var assessmentID = a.AssessmentID;
            var yearLevel = a.YearLevel;
            var semester = a.Semester;
            var period = a.Period;
            var schoolYear = a.SchoolYear;
            var amountDue = a.AmountDue || 0;
            var totalPaid = a.TotalPaid || 0;
            var balance = a.Balance || (amountDue - totalPaid);
            if (balance < 0) balance = 0;
            var status = a.Status || "Unknown";
            var paymentIDs = a.PaymentIDs || "";

            // Status badge class
            var statusClass = "";
            if (status === "Paid") statusClass = "status-paid";
            else if (status === "Unpaid") statusClass = "status-unpaid";
            else if (status === "Locked") statusClass = "status-locked";
            else if (status === "Partial") statusClass = "status-partial";

            var statusHtml = '<span class="' + statusClass + '">' + status + '</span>';

            // Payment ID display
            var paymentIdDisplay = "";
            if (paymentIDs !== "" && paymentIDs !== null) {
                paymentIdDisplay = '<span>' + paymentIDs + '</span>';
            } else {
                paymentIdDisplay = '<span>—</span>';
            }

            // Action button
            var actionBtn = "";
            if (status === "Unpaid" || status === "Partial") {
                actionBtn = '<button class="btn-pay" onclick="openPayModal(' + assessmentID + ', \'' + period + '\', ' + balance + ')">Pay</button>';
            } else if (status === "Locked") {
                actionBtn = '<span style="color:gray;">Locked</span>';
            } else if (status === "Paid") {
                actionBtn = '';
            }

            // IMPORTANT: Order MUST match table headers exactly (11 columns)
            var row = '<tr>' +
                '<td>' + assessmentID + '</td>' +           // 1. Assessment ID
                '<td>' + yearLevel + '</td>' +              // 2. Year
                '<td>' + semester + '</td>' +               // 3. Semester
                '<td>' + period + '</td>' +                 // 4. Period
                '<td>' + schoolYear + '</td>' +             // 5. School Year
                '<td>₱' + formatNumber(amountDue) + '</td>' +   // 6. Amount Due
                '<td>₱' + formatNumber(totalPaid) + '</td>' +   // 7. Paid
                '<td>₱' + formatNumber(balance) + '</td>' +     // 8. Balance
                '<td>' + statusHtml + '</td>' +              // 9. Status
                '<td>' + paymentIdDisplay + '</td>' +        // 10. Payment ID(s)
                '<td>' + actionBtn + '</td>' +               // 11. Action
                '</tr>';

            tbody.append(row);
        }
    }).fail(function (xhr, status, error) {
        console.log("Error loading assessments:", error);
        $("#assessmentTbody").html('<tr><td colspan="11">Error loading assessments.</td></tr>');
    });
}
function formatNumber(num) {
    if (num === undefined || num === null) return "0.00";
    return parseFloat(num).toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}