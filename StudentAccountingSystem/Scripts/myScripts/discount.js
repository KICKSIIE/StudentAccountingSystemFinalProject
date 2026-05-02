$(document).ready(function () {

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
    toggleDiscountFields();

    // Live search
    $('#searchDiscount').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#discountTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // Delete confirmation
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Delete this discount? This cannot be undone.')) {
            e.preventDefault();
        }
    });

});