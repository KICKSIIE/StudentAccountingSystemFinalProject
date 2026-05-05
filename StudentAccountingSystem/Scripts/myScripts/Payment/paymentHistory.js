var allData = [];
var allRefundableRows = [];
var refundTarget = null;
var currentPage = 1;
var pageSize = 16;

$(function () {
    loadStudentDropdown();
    loadSchoolYearDropdown();
    loadHistory();
});

function loadStudentDropdown() {
    $.get('/Student/GetAllStudents', function (res) {
        var sorted = res.sort(function (a, b) {
            var nameA = (a.lastname + ' ' + a.firstname).toLowerCase();
            var nameB = (b.lastname + ' ' + b.firstname).toLowerCase();
            return nameA < nameB ? -1 : nameA > nameB ? 1 : 0;
        });
        var sel = $('#fStudentID').empty().append('<option value="">Any</option>');
        sorted.forEach(function (s) {
            sel.append('<option value="' + s.idno + '">' + s.idno + ' — ' + s.lastname + ', ' + s.firstname + '</option>');
        });
    });
}

function loadSchoolYearDropdown() {
    var currentYear = new Date().getFullYear();
    var sel = $('#fSchoolYear').empty().append('<option value="">Any</option>');
    for (var i = -1; i <= 3; i++) {
        var sy = (currentYear + i) + '-' + (currentYear + i + 1);
        var selected = i === 0 ? 'selected' : '';
        sel.append('<option value="' + sy + '" ' + selected + '>' + sy + '</option>');
    }
}

function loadHistory() {
    var sid = $('#fStudentID').val();
    var sy = $('#fSchoolYear').val();
    var sem = $('#fSemester').val();
    var yl = $('#fYearLevel').val();
    var type = $('#fTransactionType').val();

    $('#historyTbody').html('<tr><td colspan="12" class="td-empty">Loading...</td></tr>');
    $('#refundableTbody').html('<tr><td colspan="7" class="td-empty">Loading...</td></tr>');

    $.get(AppUrls.getHistory, {
        studentID: sid || null,
        schoolYear: sy || null,
        semester: sem || null,
        yearLevel: yl || null
    }, function (data) {
        allData = data;
        currentPage = 1;
        populateFilters(data);

        var filtered = type === 'Refund'
            ? data.filter(function (p) { return (p.transactionType || '') === 'Refund'; })
            : type === 'Payment'
                ? data.filter(function (p) { return (p.transactionType || '') !== 'Refund'; })
                : data;
        renderHistory(filtered);
        renderRefundable(data);
    }).fail(function () {
        $('#historyTbody').html('<tr><td colspan="12" class="td-empty msg-error">Error loading data.</td></tr>');
    });
}

function populateFilters(data) {
    var semSeen = {}, ylSeen = {};
    data.forEach(function (p) {
        if (p.semester) semSeen[p.semester] = 1;
        if (p.yearLevel) ylSeen[p.yearLevel] = 1;
    });

    function repopulate(sel, sorted) {
        var cur = sel.val();
        sel.empty().append('<option value="">Any</option>');
        sorted.forEach(function (k) { sel.append('<option value="' + k + '">' + k + '</option>'); });
        if (cur) sel.val(cur);
    }

    repopulate($('#fSemester'), Object.keys(semSeen).sort());
    repopulate($('#fYearLevel'), Object.keys(ylSeen).sort());
}

// TABLE 2: Full History with pagination
function renderHistory(data) {
    var tbody = $('#historyTbody').empty();
    $('#historyCount').text(data.length);

    if (!data.length) {
        tbody.append('<tr><td colspan="12" class="td-empty">No records found.</td></tr>');
        renderPagination(0, data);
        return;
    }

    var totalPages = Math.ceil(data.length / pageSize);
    if (currentPage > totalPages) currentPage = totalPages;

    var start = (currentPage - 1) * pageSize;
    var end = Math.min(start + pageSize, data.length);
    var page = data.slice(start, end);

    page.forEach(function (p, idx) {
        var type = (p.transactionType || '');
        var isRefund = type === 'Refund';
        var badgeClass = isRefund ? 'badge badge-refund' : 'badge badge-payment';
        var badgeLabel = isRefund ? 'Refund' : 'Payment';
        var badge = '<span class="' + badgeClass + '">' + badgeLabel + '</span>';
        var dateOnly = (p.paymentDate || '—').split(' ')[0].split('T')[0];

        tbody.append('<tr>' +
            '<td>' + (start + idx + 1) + '</td>' +
            '<td>' + (p.orNumber || '—') + '</td>' +
            '<td>' + (p.studentID || '—') + '</td>' +
            '<td>' + (p.studentName || '—') + '</td>' +
            '<td>' + (p.yearLevel || '—') + '</td>' +
            '<td>' + (p.semester || '—') + '</td>' +
            '<td>' + (p.period || '—') + '</td>' +
            '<td>' + (p.schoolYear || '—') + '</td>' +
            '<td>PHP ' + fmt(p.amountPaid) + '</td>' +
            '<td>' + badge + '</td>' +
            '<td>' + dateOnly + '</td>' +
            '<td>' + (p.receivedBy || '—') + '</td>' +
            '</tr>');
    });

    renderPagination(totalPages, data);
}

function renderPagination(totalPages, data) {
    var wrap = $('#historyPaginationWrap').empty();
    if (totalPages <= 1) return;

    var html = '<div class="pagination-wrap">';
    html += '<button class="page-btn" onclick="goPage(' + (currentPage - 1) + ', filteredData)" ' +
        (currentPage === 1 ? 'disabled' : '') + '>Prev</button>';

    for (var i = 1; i <= totalPages; i++) {
        var activeClass = i === currentPage ? ' page-btn-active' : '';
        html += '<button class="page-btn' + activeClass + '" onclick="goPage(' + i + ')">' + i + '</button>';
    }

    html += '<button class="page-btn" onclick="goPage(' + (currentPage + 1) + ')" ' +
        (currentPage === totalPages ? 'disabled' : '') + '>Next</button>';
    html += '<span class="page-info">Page ' + currentPage + ' of ' + totalPages + ' (' + (data ? data.length : allData.length) + ' records)</span>';
    html += '</div>';

    wrap.html(html);
}

function goPage(page) {
    var type = $('#fTransactionType').val();
    var filtered = type === 'Refund'
        ? allData.filter(function (p) { return (p.transactionType || '') === 'Refund'; })
        : type === 'Payment'
            ? allData.filter(function (p) { return (p.transactionType || '') !== 'Refund'; })
            : allData;
    var totalPages = Math.ceil(filtered.length / pageSize);
    if (page < 1 || page > totalPages) return;
    currentPage = page;
    renderHistory(filtered);
}

// TABLE 1: Refundable Payments
function renderRefundable(data) {
    var tbody = $('#refundableTbody').empty();
    allRefundableRows = [];

    var groups = {};
    data.forEach(function (p) {
        var key = p.studentID + '|' + p.yearLevel + '|' + p.semester + '|' + p.schoolYear;
        if (!groups[key]) {
            groups[key] = {
                studentID: p.studentID,
                studentName: p.studentName,
                yearLevel: p.yearLevel,
                semester: p.semester,
                schoolYear: p.schoolYear,
                totalPaid: 0,
                hasRefund: false
            };
        }
        if (p.transactionType !== 'Refund') groups[key].totalPaid += Number(p.amountPaid);
        if (p.transactionType === 'Refund') groups[key].hasRefund = true;
    });

    var latestPerStudent = {};
    Object.values(groups).forEach(function (g) {
        var sid = g.studentID;
        if (!latestPerStudent[sid] || isLaterSem(g, latestPerStudent[sid])) {
            latestPerStudent[sid] = g;
        }
    });

    var studentsToCheck = Object.values(latestPerStudent).filter(function (g) {
        return !g.hasRefund && g.totalPaid > 0;
    });

    if (studentsToCheck.length === 0) {
        tbody.append('<tr><td colspan="7" class="td-empty">No refundable payments found.</td></tr>');
        return;
    }

    // Show a loading state while async checks run
    tbody.append('<tr id="refundLoadingRow"><td colspan="7" class="td-empty">Checking refundable payments...</td></tr>');

    var completedChecks = 0;
    var rows = 0;

    studentsToCheck.forEach(function (g) {
        $.get('/Student/GetStudentStatus', { studentID: g.studentID }, function (statusRes) {
            if (statusRes.status === 'Graduated') {
                completedChecks++;
                checkDone();
                return;
            }

            $.get('/Payment/CanRefundSemester', {
                studentID: g.studentID,
                schoolYear: g.schoolYear,
                semester: g.semester,
                yearLevel: g.yearLevel
            }, function (canRefundRes) {
                if (canRefundRes.canRefund) {
                    rows++;
                    allRefundableRows.push(g);
                    $('#refundLoadingRow').remove();
                    tbody.append('<tr' +
                        ' data-name="' + (g.studentName || '').toLowerCase() + '"' +
                        ' data-id="' + String(g.studentID || '').toLowerCase() + '">' +
                        '<td>' + g.studentID + '</td>' +
                        '<td>' + g.studentName + '</td>' +
                        '<td>Year ' + g.yearLevel + '</td>' +
                        '<td>' + g.semester + ' Sem</td>' +
                        '<td>' + g.schoolYear + '</td>' +
                        '<td>PHP ' + fmt(g.totalPaid) + '</td>' +
                        '<td><button class="btn-mini btn-mini-danger" onclick="showRefundPanel(' +
                        g.studentID + ',\'' + escQ(g.studentName) + '\',\'' +
                        g.schoolYear + '\',\'' + g.semester + '\',' + g.yearLevel +
                        ')">Refund Semester</button></td>' +
                        '</tr>');
                }
                completedChecks++;
                checkDone();
            }).fail(function () {
                completedChecks++;
                checkDone();
            });
        }).fail(function () {
            completedChecks++;
            checkDone();
        });
    });

    function checkDone() {
        if (completedChecks === studentsToCheck.length && rows === 0) {
            tbody.empty().append('<tr><td colspan="7" class="td-empty">No refundable payments found.</td></tr>');
        }
    }
}

function isLaterSem(a, b) {
    if (a.yearLevel !== b.yearLevel) return a.yearLevel > b.yearLevel;
    if (a.semester !== b.semester) return a.semester === '2nd';
    return parseInt(a.schoolYear) > parseInt(b.schoolYear);
}

function showRefundPanel(sid, name, sy, sem, yl) {
    refundTarget = {
        studentID: sid, studentName: name,
        schoolYear: sy, semester: sem, yearLevel: yl
    };

    $.get('@Url.Action("GetSemesterPaymentSummary")', {
        studentID: sid, schoolYear: sy, semester: sem, yearLevel: yl
    }, function (res) {
        $('#refundDesc').html(
            '<strong>' + name + '</strong> (ID: ' + sid + ') — ' +
            sem + ' Semester ' + sy + ', Year ' + yl);
        $('#refundTotal').text(fmt(res.totalPaid));
        $('#refundRemarks').val('');
        $('#refundPanel').show();
        $('html,body').animate({ scrollTop: $('#refundPanel').offset().top - 20 }, 300);
    });
}

$('#refundBtn').on('click', function () {
    if (!refundTarget) return;
    $('#refundModalBody').html(
        '<p style="font-size:0.85rem; color:#334155;">You are about to fully refund:</p>' +
        '<ul class="modal-list">' +
        '<li><strong>Student:</strong> ' + refundTarget.studentName + ' (ID: ' + refundTarget.studentID + ')</li>' +
        '<li><strong>Semester:</strong> ' + refundTarget.semester + ' — ' + refundTarget.schoolYear + '</li>' +
        '<li><strong>Year Level:</strong> ' + refundTarget.yearLevel + '</li>' +
        '<li><strong>Total Refund:</strong> PHP ' + $('#refundTotal').text() + '</li>' +
        '</ul>' +
        '<p class="modal-warning">' +
        'All periods in this semester will be marked as Refunded. ' +
        'Go to Assessment Management to regenerate if student wants to pay again.' +
        '</p>');
    $('#refundModal').css('display', 'flex');
});

$('#refundConfirmBtn').on('click', function () {
    $('#refundModal').hide();
    $.post('@Url.Action("ProcessRefund")', {
        studentID: refundTarget.studentID,
        schoolYear: refundTarget.schoolYear,
        semester: refundTarget.semester,
        yearLevel: refundTarget.yearLevel,
        remarks: $('#refundRemarks').val()
    }, function (res) {
        if (res.mess === 1) {
            $('#msgBox').html("<span class='msg-success'>" + res.message + "</span>");
            $('#refundPanel').hide();
            loadHistory();
        } else {
            $('#msgBox').html("<span class='msg-error'>" + res.message + "</span>");
        }
    });
});

function resetFilters() {
    $('#fStudentID').val('');
    $('#fSchoolYear').val('');
    $('#fSemester').val('');
    $('#fYearLevel').val('');
    $('#fTransactionType').val('');
    loadHistory();
}

function fmt(n) {
    return Number(n || 0).toLocaleString('en-PH',
        { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function escQ(s) { return (s || '').replace(/'/g, "\\'"); }