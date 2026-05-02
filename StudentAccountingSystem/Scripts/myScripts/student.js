$(document).ready(function () {

    // Live search
    $('#searchStudent').on('keyup', function () {
        var keyword = $(this).val().toLowerCase();
        $('#studentTable tbody tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });

    // Delete confirmation
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('Are you sure you want to delete this student?\nThis cannot be undone.')) {
            e.preventDefault();
        }
    });

});