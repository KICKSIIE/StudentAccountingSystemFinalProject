$(document).ready(function () {
    loadAdminStats();
});

function loadAdminStats() {
    // Teacher's style: $.post to the URL
    $.post("../Dashboard/GetAdminDashboardStats", {}, function (res) {
        // Teacher's style: access res[0]
        if (res[0].mess == 1) {
            $("#adminTotalAssessed").text(res[0].totalAssessed);
            $("#adminTotalPaid").text(res[0].totalPaid);
            $("#adminTotalBalance").text(res[0].totalBalance);
        } else {
            $("#result").html("<div class='alert alert-danger'>Failed to load statistics.</div>");
        }
    });
}