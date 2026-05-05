var allStudents = [];
var selectedSID = 0;
var selectedSName = "";
var pendingAction = null;

// ── Boot ─────────────────────────────────────────────────────────────
$(function () {
    loadStudents();

    $("#btnRefresh").on("click", loadStudents);
    $("#searchStudent").on("input", renderStudents);
    $("#filterStatus").on("change", renderStudents);
    $("#btnCloseDetail").on("click", closeDetail);
    $("#btnCancelConfirm").on("click", function () { $("#confirmModal").hide(); });
    $("#btnCloseBreakdown").on("click", function () { $("#breakdownModal").hide(); });

    // ONE confirm handler — attached once
    $("#confirmBtn").on("click", function () {
        if (!pendingAction) return;

        $(this).prop("disabled", true);
        $("#confirmModal").hide();

        var action = pendingAction;
        pendingAction = null;

        $.post(action.url, action.postData)
            .done(function (res) {
                if (typeof res === "string") {
                    showToast("Server returned an unexpected response.", false);
                    return;
                }
                showToast(res.message, res.mess === 1);
                loadStudents();
                if (res.mess === 1) {
                    loadDetail(action.sid, action.studentName);
                }
            })
            .fail(function (xhr) {
                showToast("Request failed (" + xhr.status + "). Please try again.", false);
            })
            .always(function () {
                $("#confirmBtn").prop("disabled", false);
            });
    });
});

// ── Toast ─────────────────────────────────────────────────────────────
function showToast(msg, success) {
    var $t = $("#toastMsg");
    $t.removeClass("toast-success toast-error")
        .addClass(success ? "toast-success" : "toast-error")
        .text(msg)
        .fadeIn(200);
    setTimeout(function () { $t.fadeOut(400); }, 3500);
}

// ── Load & Render Students ────────────────────────────────────────────
function loadStudents() {
    $("#studentTbody").html('<tr><td colspan="9" class="td-empty">Loading...</td></tr>');
    $.get(AppUrls.getStudentAssessmentStatus, function (data) {
        allStudents = data;
        renderStudents();
    }).fail(function () {
        $("#studentTbody").html('<tr><td colspan="9" class="td-empty msg-error">Error loading students.</td></tr>');
    });
}

function renderStudents() {
    var search = $("#searchStudent").val().toLowerCase();
    var filter = $("#filterStatus").val();
    var tbody = $("#studentTbody").empty();

    var filtered = allStudents.filter(function (s) {
        var matchSearch = !search ||
            s.StudentID.toString().includes(search) ||
            s.StudentName.toLowerCase().includes(search);

        var matchFilter = true;
        if (filter === "can_generate") matchFilter = s.CanGenerate;
        if (filter === "next_ready") matchFilter = s.NextCanGenerate && !s.IsGraduating && !s.NeedsRegenerate;
        if (filter === "regenerate") matchFilter = s.NeedsRegenerate;

        return matchSearch && matchFilter;
    }).sort(function (a, b) {
        return a.StudentName.localeCompare(b.StudentName);
    });

    if (!filtered.length) {
        tbody.append('<tr><td colspan="9" class="td-empty">No students found.</td></tr>');
        return;
    }

    filtered.forEach(function (s, index) {
        var safeName = s.StudentName.replace(/'/g, "\\'");

        var currentCell = s.CanGenerate
            ? '<button class="btn-mini btn-generate" onclick="event.stopPropagation(); confirmGenerate(' + s.StudentID + ', false)">Generate</button>'
            : '<span class="badge badge-generated">Generated</span>';

        var nextCell = "";
        if (s.NeedsRegenerate) {
            nextCell = '<button class="btn-mini btn-regenerate" onclick="event.stopPropagation(); confirmGenerate(' + s.StudentID + ', false, true)">Regenerate</button>';
        } else if (s.IsGraduating) {
            nextCell = '<span class="td-muted" style="font-size:0.8rem;">—</span>';
        } else if (s.NextCanGenerate) {
            nextCell = '<button class="btn-mini btn-next" onclick="event.stopPropagation(); confirmGenerate(' + s.StudentID + ', true)">Generate Next</button>';
        } else {
            nextCell = '<span class="badge badge-locked">Locked</span>';
        }

        tbody.append(
            '<tr class="student-row" onclick="loadDetail(' + s.StudentID + ', \'' + safeName + '\')">' +
            '<td class="td-center">' + (index + 1) + '</td>' +
            '<td>' + s.StudentID + '</td>' +
            '<td>' + s.StudentName + '</td>' +
            '<td>' + s.CourseCode + '</td>' +
            '<td class="td-center">' + s.YearLevel + '</td>' +
            '<td class="td-center">' + s.Semester + '</td>' +
            '<td>' + s.SchoolYear + '</td>' +
            '<td class="td-center">' + currentCell + '</td>' +
            '<td class="td-center">' + nextCell + '</td>' +
            '</tr>'
        );
    });
}

// ── Detail Panel ──────────────────────────────────────────────────────
function loadDetail(sid, name) {
    selectedSID = sid;
    selectedSName = name;
    $("#detailTitle").text("Assessments — " + name + " (ID: " + sid + ")");
    $("#detailPanel").show();
    $("html,body").animate({ scrollTop: $("#detailPanel").offset().top - 20 }, 300);

    $("#assessmentTbody").html('<tr><td colspan="9" class="td-empty">Loading assessments...</td></tr>');

    $.get(AppUrls.getStudentAssessments, { studentID: sid }, function (data) {
        data.sort(function (a, b) { return a.AssessmentID - b.AssessmentID; });

        var tbody = $("#assessmentTbody").empty();

        if (!data.length) {
            tbody.append('<tr><td colspan="9" class="td-empty">No assessments generated yet.</td></tr>');
            return;
        }

        data.forEach(function (a) {
            var isRefunded = a.Status === "Refunded";
            var statusKey = a.Status.toLowerCase().replace("-", "");
            var statusBadge = '<span class="badge badge-' + statusKey + '">' + a.Status + '</span>';
            var rowClass = isRefunded ? " class=\"row-refunded\"" : "";

            var breakdownBtn = '<button class="btn-mini btn-breakdown" onclick="showBreakdown(' +
                sid + ', \'' + a.Period + '\', \'' + a.SchoolYear + '\', \'' + a.Semester + '\', ' + a.YearLevel +
                ')">View</button>';

            tbody.append(
                '<tr' + rowClass + '>' +
                '<td>Year ' + a.YearLevel + '</td>' +
                '<td>' + a.Semester + '</td>' +
                '<td>' + a.Period + '</td>' +
                '<td>' + a.SchoolYear + '</td>' +
                '<td>PHP ' + fmt(a.AmountDue) + '</td>' +
                '<td>PHP ' + fmt(a.TotalPaid) + '</td>' +
                '<td>PHP ' + fmt(a.Balance) + '</td>' +
                '<td>' + statusBadge + '</td>' +
                '<td class="td-center">' + breakdownBtn + '</td>' +
                '</tr>'
            );
        });
    }).fail(function () {
        $("#assessmentTbody").html('<tr><td colspan="9" class="td-empty msg-error">Error loading assessments.</td></tr>');
    });
}

function closeDetail() {
    $("#detailPanel").hide();
}

// ── Fee Breakdown Modal ───────────────────────────────────────────────
function showBreakdown(sid, period, sy, sem, yl) {
    $.get(AppUrls.getStudentFeeBreakdown, { studentID: sid }, function (res) {
        if (!res || res.mess === 0) {
            showToast("Could not load fee breakdown.", false);
            return;
        }

        $("#breakdownTitle").text("Fee Breakdown — " + period + " | " + sem + " Sem " + sy + " Year " + yl);

        var rows = [
            '<tr class="section-header"><td colspan="2">Course</td></tr>',
            '<tr><td>' + res.courseCode + " — " + res.courseName + "</td><td></td></tr>",
            '<tr class="section-header"><td colspan="2">Fee Components</td></tr>',
            "<tr><td>Tuition (" + res.units + " units x PHP " + fmt(res.tuitionPerUnit) + ")</td><td>PHP " + fmt(res.tuitionFee) + "</td></tr>",
            "<tr><td>Registration Fee</td><td>PHP " + fmt(res.regFee) + "</td></tr>",
            "<tr><td>Miscellaneous Fee</td><td>PHP " + fmt(res.miscFee) + "</td></tr>",
            "<tr><td>Laboratory Fee</td><td>PHP " + fmt(res.labFee) + "</td></tr>",
            '<tr class="total-row"><td>Total Fee</td><td>PHP ' + fmt(res.totalFee) + "</td></tr>"
        ];

        if (res.schName) {
            rows.push('<tr class="discount-row"><td>Scholarship: ' + res.schName + " (" + res.schDetail + ")</td><td>-PHP " + fmt(res.schDiscount) + "</td></tr>");
        }
        if (res.disName) {
            rows.push('<tr class="discount-row"><td>Discount: ' + res.disName + " (" + res.disDetail + ")</td><td>-PHP " + fmt(res.disDiscount) + "</td></tr>");
        }

        rows.push('<tr class="net-row"><td>Net Assessment</td><td>PHP ' + fmt(res.netAssessment) + "</td></tr>");
        rows.push('<tr class="section-header"><td colspan="2">Period Breakdown</td></tr>');

        var periods = [
            { key: "Prelim", label: "Prelim (53%)", amt: res.prelimAmt },
            { key: "Midterm", label: "Midterm (11%)", amt: res.midtermAmt },
            { key: "Semi-Final", label: "Semi-Final (11%)", amt: res.semiFinalAmt },
            { key: "Final", label: "Final (25%)", amt: res.finalAmt }
        ];

        periods.forEach(function (p) {
            var activeClass = p.key === period ? " period-active" : " period-row";
            rows.push('<tr class="' + activeClass + '"><td>' + p.label + "</td><td>PHP " + fmt(p.amt) + "</td></tr>");
        });

        $("#breakdownContent").html('<table class="breakdown-table">' + rows.join("") + "</table>");
        $("#breakdownModal").css("display", "flex");
    }).fail(function () {
        showToast("Error loading fee breakdown.", false);
    });
}

// ── Confirm Modal ─────────────────────────────────────────────────────
function confirmGenerate(sid, isNext, isRegenerate) {
    var s = allStudents.find(function (x) { return x.StudentID == sid; });
    if (!s) return;

    var title, sem, sy, yr, url, extraNotice = "";
    var postData = { studentID: sid };

    if (isRegenerate) {
        title = "Regenerate Assessments";
        sem = s.RefundedSem;
        yr = s.RefundedYL;
        sy = s.RefundedSY;
        url = AppUrls.regenerateAfterRefund;
        postData.schoolYear = s.RefundedSY;
        postData.semester = s.RefundedSem;
        postData.yearLevel = s.RefundedYL;
        extraNotice = '<p class="modal-notice modal-notice-warn">Previous refunded assessments remain as records. New assessments will be created for Year ' + yr + ', ' + sem + ' Semester ' + sy + '.</p>';
    } else if (isNext) {
        title = "Generate Next Semester";
        sem = s.NextSem;
        yr = s.NextYear;
        sy = s.NextSY;
        url = AppUrls.generateNextSemester;
    } else {
        title = "Generate Current Semester";
        sem = s.Semester;
        yr = s.YearLevel;
        sy = s.SchoolYear;
        url = AppUrls.generateAssessment;
    }

    $("#confirmTitle").text(title);
    $("#confirmBody").html(
        '<table class="confirm-info">' +
        '<tr><td class="info-label">Student</td><td><strong>' + s.StudentName + " (ID: " + sid + ")</strong></td></tr>" +
        "<tr><td class=\"info-label\">Year Level</td><td>" + yr + "</td></tr>" +
        "<tr><td class=\"info-label\">Semester</td><td>" + sem + "</td></tr>" +
        "<tr><td class=\"info-label\">School Year</td><td>" + sy + "</td></tr>" +
        "</table>" +
        '<p class="modal-notice">This will create 4 assessment periods: Prelim, Midterm, Semi-Final, Final.</p>' +
        extraNotice
    );

    pendingAction = { url: url, postData: postData, sid: sid, studentName: s.StudentName };

    $("#confirmBtn").prop("disabled", false);
    $("#confirmModal").css("display", "flex");
}

// ── Helpers ───────────────────────────────────────────────────────────
function fmt(n) {
    return Number(n || 0).toLocaleString("en-PH", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}
