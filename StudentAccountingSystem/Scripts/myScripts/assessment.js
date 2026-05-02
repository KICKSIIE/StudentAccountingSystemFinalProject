$(document).ready(function () {

    // ── LIVE SEARCH (Index page) ──────────────────────────────────────
    $('#searchAssessment').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#assessmentTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // ── FEE PREVIEW (Create page) ─────────────────────────────────────
    function updatePreview() {
        var student = $('#StudentID').val();
        var sy = $('#SchoolYear').val().trim();
        var sem = $('#Semester').val();
        var period = $('#Period').val();

        // Only call if all 4 fields are filled
        if (!student || !sy || !sem || !period) {
            $('#feePreview').hide();
            return;
        }

        $.ajax({
            url: '/Assessment/GetFeePreview',
            type: 'GET',
            data: {
                studentID: student,
                schoolYear: sy,
                semester: sem,
                period: period
            },
            success: function (data) {
                if (!data || data.error) {
                    $('#feePreview').hide();
                    $('#noFeeAlert').show();
                    return;
                }
                $('#noFeeAlert').hide();

                $('#prevTuition').text('₱' + formatNum(data.tuitionFee));
                $('#prevReg').text('₱' + formatNum(data.registrationFee));
                $('#prevMisc').text('₱' + formatNum(data.miscFee));
                $('#prevLab').text('₱' + formatNum(data.labFee));
                $('#prevTotal').text('₱' + formatNum(data.totalFee));
                $('#prevDiscount').text('- ₱' + formatNum(data.discountAmount));
                $('#prevNet').text('₱' + formatNum(data.netAssessment));
                $('#prevPercent').text(data.percentRequired);
                $('#prevDue').text('₱' + formatNum(data.amountDue));

                $('#feePreview').slideDown(200);
            },
            error: function () {
                $('#feePreview').hide();
            }
        });
    }

    // Format numbers with commas e.g. 11,950.00
    function formatNum(n) {
        return parseFloat(n).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    // Trigger preview when any field changes
    $('#StudentID, #Semester, #Period').on('change', updatePreview);
    $('#SchoolYear').on('blur', updatePreview); // blur so it waits for full input

    // ── DELETE CONFIRMATION ───────────────────────────────────────────
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Delete this assessment? This cannot be undone.')) {
            e.preventDefault();
        }
    });

});