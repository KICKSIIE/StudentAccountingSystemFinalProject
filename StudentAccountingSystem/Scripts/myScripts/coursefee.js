$(document).ready(function () {

    // ── LIVE SEARCH (Index) ───────────────────────────────────────────
    $('#searchFee').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#feeTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // ── DELETE CONFIRMATION ───────────────────────────────────────────
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Delete this fee schedule? Existing assessments using this schedule will be affected.')) {
            e.preventDefault();
        }
    });

    // ── LIVE TOTAL PREVIEW (Create & Edit) ───────────────────────────
    function formatPHP(n) {
        return '₱' + parseFloat(n || 0).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function updateTotal() {
        var units = parseFloat($('#Units').val()) || 0;
        var tpu = parseFloat($('#TuitionPerUnit').val()) || 0;
        var reg = parseFloat($('#RegistrationFee').val()) || 0;
        var misc = parseFloat($('#MiscellaneousFee').val()) || 0;
        var lab = parseFloat($('#LaboratoryFee').val()) || 0;

        var tuition = units * tpu;
        var total = tuition + reg + misc + lab;

        $('#previewUnits').text(units);
        $('#previewRate').text(tpu.toFixed(2));
        $('#previewTuition').text(formatPHP(tuition));
        $('#previewReg').text(formatPHP(reg));
        $('#previewMisc').text(formatPHP(misc));
        $('#previewLab').text(formatPHP(lab));
        $('#previewTotal').text(formatPHP(total));
    }

    // Trigger on any fee field change
    $('#Units, #TuitionPerUnit, #RegistrationFee, #MiscellaneousFee, #LaboratoryFee')
        .on('input change', updateTotal);

    // Run once on page load (Edit page already has values)
    updateTotal();
});