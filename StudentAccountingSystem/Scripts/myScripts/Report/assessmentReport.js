$(function () {
    loadReport();
});

function loadReport() {
    var sy = $("#selSchoolYear").val();
    var sem = $("#selSemester").val();

    $("#msgBox").html("");
    $("#divReport").hide();

    $.post(AppUrls.getAssessmentReport, {
        schoolYear: sy,
        semester: sem,
        courseID: $("#selCourse").val(),
        yearLevel: $("#selYear").val(),
        period: $("#selPeriod").val(),
        status: $("#selStatus").val()
    }, function (res) {
        if (!res || res.length === 0) {
            $("#msgBox").html("<span class='msg-warning'>No records found.</span>");
            return;
        }

        if (res[0].mess === 0) {
            $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
            return;
        }

        var syLabel = sy || "All Years";
        var semLabel = sem ? sem + " Semester" : "All Semesters";
        $("#reportTitle").text("Assessment Report — " + syLabel + " | " + semLabel);

        var html = "";
        var sumDue = 0;
        var sumPaid = 0;
        var sumBal = 0;

        res.forEach(function (r, i) {
            var bal = parseFloat(r.balance);
            sumDue += parseFloat(r.amountDue);
            sumPaid += parseFloat(r.totalPaid);
            sumBal += bal;

            var badgeClass = {
                "Paid": "badge-paid",
                "Partial": "badge-partial",
                "Unpaid": "badge-unpaid",
                "Locked": "badge-locked"
            }[r.status] || "badge-locked";

            var balClass = bal > 0 ? "amt-negative" : "amt-positive";

            html +=
                "<tr>" +
                "<td>" + (i + 1) + "</td>" +
                "<td>" + r.studentID + "</td>" +
                "<td>" + r.studentName + "</td>" +
                "<td>" + r.courseCode + "</td>" +
                "<td>Year " + r.yearLevel + "</td>" +
                "<td><span class='badge badge-period'>" + r.period + "</span></td>" +
                "<td class='td-right'>PHP " + fmt(r.tuitionFee) + "</td>" +
                "<td class='td-right'>PHP " + fmt(r.registrationFee) + "</td>" +
                "<td class='td-right'>PHP " + fmt(r.miscFee) + "</td>" +
                "<td class='td-right'>PHP " + fmt(r.labFee) + "</td>" +
                "<td class='td-right amt-positive'>PHP " + fmt(r.discountAmount) + "</td>" +
                "<td class='td-right' style='font-weight:700;'>PHP " + fmt(r.netAssessment) + "</td>" +
                "<td class='td-right'>PHP " + fmt(r.amountDue) + "</td>" +
                "<td class='td-right amt-positive'>PHP " + fmt(r.totalPaid) + "</td>" +
                "<td class='td-right " + balClass + "'>PHP " + fmt(bal) + "</td>" +
                "<td><span class='badge " + badgeClass + "'>" + r.status + "</span></td>" +
                "</tr>";
        });

        $("#reportBody").html(html);
        $("#sumAmountDue").text("PHP " + fmt(sumDue));
        $("#sumTotalPaid").text("PHP " + fmt(sumPaid));
        $("#sumBalance").text("PHP " + fmt(sumBal));
        $("#divReport").show();
    }).fail(function () {
        $("#msgBox").html("<span class='msg-error'>Error loading report. Please try again.</span>");
    });
}

function printReport() {
    var content = document.getElementById("divReport").innerHTML;
    var original = document.body.innerHTML;
    document.body.innerHTML = "<h2>Assessment Report</h2><hr/>" + content;
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
