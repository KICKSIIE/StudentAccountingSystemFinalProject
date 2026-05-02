$(document).ready(function () {

    // Live search on Index
    $('#searchCourse').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#courseTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // Delete confirmation
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Delete this course? This cannot be undone.')) {
            e.preventDefault();
        }
    });

    // Live fee total preview (Create & Edit)
    function fmt(n) {
        return '₱' + parseFloat(n || 0).toLocaleString('en-PH', {
            minimumFractionDigits: 2, maximumFractionDigits: 2
        });
    }

    function updateTotal() {
        var units = parseFloat($('#Units').val()) || 0;
        var tpu = parseFloat($('#TuitionPerUnit').val()) || 0;
        var reg = parseFloat($('#RegistrationFee').val()) || 0;
        var misc = parseFloat($('#MiscFee').val()) || 0;
        var lab = parseFloat($('#LabFee').val()) || 0;

        var tuition = units * tpu;
        var total = tuition + reg + misc + lab;

        $('#previewUnits').text(units);
        $('#previewRate').text(tpu.toFixed(2));
        $('#previewTuition').text(fmt(tuition));
        $('#previewReg').text(fmt(reg));
        $('#previewMisc').text(fmt(misc));
        $('#previewLab').text(fmt(lab));
        $('#previewTotal').text(fmt(total));
    }

    $('#Units, #TuitionPerUnit, #RegistrationFee, #MiscFee, #LabFee')
        .on('input change', updateTotal);

    updateTotal(); // run once on load for Edit page
});