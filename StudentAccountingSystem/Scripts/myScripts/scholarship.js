$(document).ready(function () {

    // Show/hide percent or amount field based on type selection
    function toggleDiscountFields() {
        var type = $('#discountType').val();
        if (type === 'Percent') {
            $('#percentGroup').show();
            $('#amountGroup').hide();
        } else if (type === 'Fixed') {
            $('#percentGroup').hide();
            $('#amountGroup').show();
        } else {
            $('#percentGroup').hide();
            $('#amountGroup').hide();
        }
    }

    $('#discountType').on('change', toggleDiscountFields);
    toggleDiscountFields(); // run on page load for Edit page

    // Live search
    $('#searchScholarship').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#scholarshipTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // Delete confirmation
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Delete this scholarship? This cannot be undone.')) {
            e.preventDefault();
        }
    });

});