$(document).ready(function () {

    // ── LIVE SEARCH ───────────────────────────────────────────────────
    $('#searchPayment').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#paymentTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // ── BALANCE PANEL ─────────────────────────────────────────────────
    $('#AssessmentID').on('change', function () {
        var id = $(this).val();
        if (!id) { $('#balancePanel').hide(); return; }

        $.ajax({
            url: '/Payment/GetBalance',
            type: 'GET',
            data: { assessmentID: id },
            success: function (data) {
                if (!data || data.error) { $('#balancePanel').hide(); return; }

                // Get total already paid via separate call
                $.ajax({
                    url: '/Payment/GetTotalPaid',
                    type: 'GET',
                    data: { assessmentID: id },
                    success: function (paid) {
                        var remaining = data.amountDue - paid;

                        $('#balStudent').text(data.studentName);
                        $('#balCourse').text(data.courseName);
                        $('#balPeriod').text(data.period + ' — ' + data.schoolYear + ' ' + data.semester + ' Sem');
                        $('#balDue').text('₱' + formatNum(data.amountDue));
                        $('#balPaid').text('₱' + formatNum(paid));
                        $('#balRemaining').text('₱' + formatNum(remaining));

                        // Auto-fill max payable amount
                        $('#AmountPaid').val(remaining.toFixed(2));
                        $('#AmountPaid').attr('max', remaining.toFixed(2));

                        $('#balancePanel').slideDown(200);
                    }
                });
            }
        });
    });

    function formatNum(n) {
        return parseFloat(n).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }
});