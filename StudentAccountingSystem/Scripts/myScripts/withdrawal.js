$(document).ready(function () {

    $(document).on('click', '.btn-approve', function (e) {
        if (!confirm('Approve this withdrawal?\nThis will cancel remaining assessments and cannot be undone.')) {
            e.preventDefault();
        }
    });

    $(document).on('click', '.btn-reject', function (e) {
        if (!confirm('Reject this withdrawal request?')) {
            e.preventDefault();
        }
    });

});