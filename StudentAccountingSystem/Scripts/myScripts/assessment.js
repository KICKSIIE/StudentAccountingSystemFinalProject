$(document).ready(function () {

    // Combined filter — search + status + period
    function filterTable() {
        var keyword = $('#searchAssessment').val().toLowerCase();
        var status = $('#filterStatus').val().toLowerCase();
        var period = $('#filterPeriod').val().toLowerCase();

        $('#assessmentTable tbody tr').each(function () {
            var rowText = $(this).text().toLowerCase();
            var rowStatus = $(this).data('status').toLowerCase();
            var rowPeriod = $(this).data('period').toLowerCase();

            var matchText = rowText.indexOf(keyword) > -1;
            var matchStatus = status === '' || rowStatus === status;
            var matchPeriod = period === '' || rowPeriod === period;

            $(this).toggle(matchText && matchStatus && matchPeriod);
        });
    }

    $('#searchAssessment').on('keyup', filterTable);
    $('#filterStatus').on('change', filterTable);
    $('#filterPeriod').on('change', filterTable);

    // Unlock confirmation
    $(document).on('click', '.btn-unlock', function (e) {
        if (!confirm('Manually unlock this assessment?\n' +
            'This overrides the sequential payment rule.')) {
            e.preventDefault();
        }
    });

});