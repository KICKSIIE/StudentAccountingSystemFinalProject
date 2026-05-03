document.addEventListener('DOMContentLoaded', function () {
    setTimeout(function () {
        var alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function (alert) {
            alert.style.transition = 'opacity 1s';
            alert.style.opacity = '0';
            setTimeout(function () { alert.style.display = 'none'; }, 1000);
        });
    }, 4000);
});

function showWithdrawalModal() {
    var modal = document.getElementById('withdrawalModal');
    if (modal) modal.style.display = 'flex';
}

function showReactivationModal() {
    var modal = document.getElementById('reactivationModal');
    if (!modal) { alert('Modal not found.'); return; }

    var currentSem = document.getElementById('currentSemester');
    var currentSY = document.getElementById('currentSchoolYear');
    var currentYL = document.getElementById('currentYearLevel');

    if (!currentSem || !currentSY || !currentYL) {
        alert('Missing student info hidden fields in Index.cshtml');
        return;
    }

    var sem = currentSem.value;
    var sy = currentSY.value;
    var yearLevel = parseInt(currentYL.value);
    var syParts = sy.split('-');
    var startYear = parseInt(syParts[0]);

    var targetSem, targetSY, targetYearLevel;

    if (sem === '1st') {
        targetSem = '1st';
        targetSY = (startYear + 1) + '-' + (startYear + 2);
        targetYearLevel = yearLevel;
    } else {
        targetSem = '1st';
        targetSY = (startYear + 1) + '-' + (startYear + 2);
        targetYearLevel = yearLevel + 1;
        if (targetYearLevel > 4) targetYearLevel = 4;
    }

    // Show calculated values in modal
    var elYL = document.getElementById('reactYearLevel');
    var elSem = document.getElementById('reactSemester');
    var elSY = document.getElementById('reactSchoolYear');
    if (elYL) elYL.textContent = 'Year ' + targetYearLevel;
    if (elSem) elSem.textContent = targetSem + ' Semester';
    if (elSY) elSY.textContent = targetSY;

    // Store values in hidden fields
    var hSY = document.getElementById('hiddenTargetSY');
    var hSem = document.getElementById('hiddenTargetSem');
    var hYL = document.getElementById('hiddenTargetYearLevel');

    if (!hSY || !hSem || !hYL) {
        alert('Hidden form fields missing. Check reactivation form in Index.cshtml');
        return;
    }

    hSY.value = targetSY;
    hSem.value = targetSem;
    hYL.value = targetYearLevel;

    modal.style.display = 'flex';
}

function submitReactivation() {
    var hSY = document.getElementById('hiddenTargetSY');
    var hSem = document.getElementById('hiddenTargetSem');
    var hYL = document.getElementById('hiddenTargetYearLevel');
    var form = document.getElementById('reactivationForm');

    if (!hSY || !hSem || !hYL) {
        alert('Hidden fields not found. Make sure the form is in Index.cshtml');
        return;
    }

    if (!hSY.value || !hSem.value || !hYL.value) {
        alert('Please open the modal first before submitting.');
        return;
    }

    if (!form) {
        alert('Reactivation form element not found.');
        return;
    }

    form.submit();
}

function submitReactivationDirect() {
    // Create a temporary form and submit it directly
    var form = document.createElement('form');
    form.method = 'POST';
    form.action = '/StudentPortal/RequestReactivation';

    // Get the antiforgery token from any existing form on the page
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        var tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = token.value;
        form.appendChild(tokenInput);
    }

    document.body.appendChild(form);
    form.submit();
}