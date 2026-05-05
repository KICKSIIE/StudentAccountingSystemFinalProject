$(function () {
    loadDashboard();
});

function loadDashboard() {
    $.get('/Dashboard/GetDashboardData', function (res) {
        if (!res || !res.length || res[0].mess !== 1) return;
        var d = res[0];

        /* Bind Financial Data */
        $('#statAssessed').text(d.totalAssessed);
        $('#statCollected').text(d.totalCollected);
        $('#statBalance').text(d.outstandingBal);
        $('#statRefunded').text(d.totalRefunded);

        /* Bind Student Data */
        $('#statRegistered').text(d.totalRegistered);
        $('#statEnrolled').text(d.totalEnrolled);
        $('#statGraduated').text(d.totalGraduated);
    });
}