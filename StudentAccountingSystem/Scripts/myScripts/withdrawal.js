$(document).ready(function () {
    $(document).on('click', '.btn-approve', function (e) {
        if (!confirm('Approve this withdrawal request?\nThis will set the student status to Withdrawn.')) {
            e.preventDefault();
        }
    });
    $(document).on('click', '.btn-reject', function (e) {
        if (!confirm('Reject this withdrawal request?')) {
            e.preventDefault();
        }
    });
});