function loadLedger() {
    var sid = $("#selStudent").val();
    if (!sid) {
        $("#msgBox").html("<span class='msg-warning'>Please select a student.</span>");
        return;
    }

    $("#msgBox").html("");
    $("#divLedger").hide();

    $.post(AppUrls.getStudentLedger, { studentID: sid }, function (res) {
        if (!res || res.length === 0) {
            $("#msgBox").html("<span class='msg-warning'>No records found.</span>");
            return;
        }

        if (res[0].mess === 0) {
            $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
            return;
        }

        var d = res[0];
        var info = d.studentInfo;

        // Fill student info
        $("#lblName").text(info.fullName);
        $("#lblID").text(info.studentID);
        $("#lblCourse").text(info.course);
        $("#lblStatus").text(info.status);
        $("#lblYear").text("Year " + info.yearLevel);
        $("#lblSem").text(info.semester);
        $("#lblSY").text(info.schoolYear);
        $("#lblScholarship").text(info.scholarship || "—");
        $("#lblDiscount").text(info.discount || "—");

        var html = "";
        var runningBalance = 0;
        var totalDebits = 0;
        var totalCredits = 0;

        // Assessment rows with expandable fee breakdown
        d.assessments.forEach(function (a) {
            runningBalance += parseFloat(a.amountDue);
            totalDebits += parseFloat(a.amountDue);

            var balance = parseFloat(a.amountDue) - parseFloat(a.totalPaid);
            var balClass = balance > 0 ? "amt-negative" : "amt-positive";

            var badgeClass = {
                "Paid": "badge-paid",
                "Partial": "badge-partial",
                "Locked": "badge-locked",
                "Unpaid": "badge-unpaid"
            }[a.status] || "badge-unpaid";

            var cumulative = { "Prelim": 53, "Midterm": 64, "Semi-Final": 75, "Final": 100 };
            var pct = parseFloat(a.percentRequired).toFixed(0);
            var cum = cumulative[a.period] || pct;

            // Charge row
            html +=
                "<tr class='row-charge' onclick='toggleDetail(" + a.assessmentID + ")'>" +
                "<td>" + a.assessedAt + "</td>" +
                "<td>" + a.schoolYear + "</td>" +
                "<td>" + a.semester + "</td>" +
                "<td>Year " + a.yearLevel + "</td>" +
                "<td>Charge: " + a.period + " Assessment" +
                " <small class='expand-hint'>[ID: " + a.assessmentID + "] — click to expand</small></td>" +
                "<td class='td-right amt-debit'>PHP " + fmt(a.amountDue) + "</td>" +
                "<td class='td-right'>—</td>" +
                "<td class='td-right' style='font-weight:700;'>PHP " + fmt(runningBalance) + "</td>" +
                "</tr>";

            // Expandable detail row
            html +=
                "<tr id='detail_" + a.assessmentID + "' class='row-detail' style='display:none;'>" +
                "<td colspan='8'>" +
                "<div class='detail-inner'>" +

                // Left: Fee Breakdown
                "<div>" +
                "<p class='detail-section-title'>Fee Breakdown</p>" +
                "<table class='detail-table'>" +
                "<tr><td>Tuition (" + a.units + " units x PHP " + fmt(a.tuitionPerUnit) + ")</td><td class='td-right'>PHP " + fmt(a.tuitionFee) + "</td></tr>" +
                "<tr><td>Registration Fee</td><td class='td-right'>PHP " + fmt(a.registrationFee) + "</td></tr>" +
                "<tr><td>Miscellaneous Fee</td><td class='td-right'>PHP " + fmt(a.miscFee) + "</td></tr>" +
                "<tr><td>Laboratory Fee</td><td class='td-right'>PHP " + fmt(a.labFee) + "</td></tr>" +
                "<tr class='detail-total'><td>Total Fee</td><td class='td-right'>PHP " + fmt(a.totalFee) + "</td></tr>" +
                (parseFloat(a.discountAmount) > 0
                    ? "<tr class='detail-discount'><td>Discount Applied</td><td class='td-right'>- PHP " + fmt(a.discountAmount) + "</td></tr>"
                    : "") +
                "<tr class='detail-net'><td>Net Assessment</td><td class='td-right'>PHP " + fmt(a.netAssessment) + "</td></tr>" +
                "</table>" +
                "</div>" +

                // Right: Period Quota
                "<div>" +
                "<p class='detail-section-title'>Period Quota</p>" +
                "<table class='detail-table'>" +
                "<tr><td>Period</td><td><strong>" + a.period + "</strong></td></tr>" +
                "<tr><td>% Required</td><td><strong>" + pct + "% of period (" + cum + "% cumulative)</strong></td></tr>" +
                "<tr><td>Net Assessment</td><td>PHP " + fmt(a.netAssessment) + "</td></tr>" +
                "<tr class='detail-due'><td>Amount Due</td><td class='td-right'>PHP " + fmt(a.amountDue) + "</td></tr>" +
                "<tr class='detail-paid'><td>Total Paid</td><td class='td-right'>PHP " + fmt(a.totalPaid) + "</td></tr>" +
                "<tr><td>Balance</td><td class='td-right " + balClass + "'>PHP " + fmt(balance) + "</td></tr>" +
                "<tr><td>Status</td><td><span class='badge " + badgeClass + "'>" + a.status + "</span></td></tr>" +
                (parseFloat(a.overpaidAmount) > 0
                    ? "<tr><td>Credit Balance</td><td class='td-right amt-positive'>PHP " + fmt(a.overpaidAmount) + "</td></tr>"
                    : "") +
                "</table>" +
                "</div>" +

                "</div></td></tr>";

            // Payment rows under this assessment
            d.payments.forEach(function (p) {
                if (p.assessmentID != a.assessmentID) return;
                if (p.transactionType === "Refund") return;

                runningBalance -= parseFloat(p.amountPaid);
                totalCredits += parseFloat(p.amountPaid);

                html +=
                    "<tr class='row-payment'>" +
                    "<td>" + p.paymentDate + "</td>" +
                    "<td>" + p.schoolYear + "</td>" +
                    "<td>" + p.semester + "</td>" +
                    "<td>Year " + p.yearLevel + "</td>" +
                    "<td>Payment <small class='expand-hint'>[Payment ID: " + p.orNumber + "]</small></td>" +
                    "<td class='td-right'>—</td>" +
                    "<td class='td-right amt-credit'>PHP " + fmt(p.amountPaid) + "</td>" +
                    "<td class='td-right' style='font-weight:700;'>PHP " + fmt(runningBalance) + "</td>" +
                    "</tr>";
            });
        });

        // Refund info rows — informational only, no balance effect
        d.refunds.forEach(function (ref) {
            if (ref.status !== "Approved") return;

            html +=
                "<tr class='row-refund'>" +
                "<td>" + ref.requestDate + "</td>" +
                "<td colspan='3'></td>" +
                "<td>Refund — Cash returned to student</td>" +
                "<td class='td-right'>—</td>" +
                "<td class='td-right'>PHP " + fmt(ref.amount) + "</td>" +
                "<td class='td-right'>—</td>" +
                "</tr>";
        });

        $("#ledgerBody").html(html);
        $("#totalDebits").text("PHP " + fmt(totalDebits));
        $("#totalCredits").text("PHP " + fmt(totalCredits));
        $("#finalBalance").text("PHP " + fmt(runningBalance));
        $("#divLedger").show();

    }).fail(function () {
        $("#msgBox").html("<span class='msg-error'>Error loading ledger. Please try again.</span>");
    });
}

function toggleDetail(assessmentID) {
    $("#detail_" + assessmentID).toggle();
}

function printLedger() {
    var content = document.getElementById("divLedger").innerHTML;
    var original = document.body.innerHTML;
    document.body.innerHTML = content;
    window.print();
    document.body.innerHTML = original;
    location.reload();
}

function fmt(n) {
    return Number(n || 0).toLocaleString("en-PH", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}
