$(function () { loadAdmins(); });

function createAdmin() {
    var u = $("#txtUsername").val().trim();
    var p = $("#txtPassword").val();
    var c = $("#txtConfirm").val();

    if (!u || !p) { showMsg("createMsg", "error", "Username and password are required."); return; }
    if (p !== c) { showMsg("createMsg", "error", "Passwords do not match."); return; }
    if (p.length < 6) { showMsg("createMsg", "error", "Password must be at least 6 characters."); return; }

    $.post("/Admin/CreateAdmin", { username: u, password: p }, function (res) {
        if (res.mess === 1) {
            showMsg("createMsg", "success", "Admin created. User ID: " + res.userID);
            $("#txtUsername, #txtPassword, #txtConfirm").val("");
            loadAdmins();
        } else {
            showMsg("createMsg", "error", res.message);
        }
    });
}

function loadAdmins() {
    $.get("/Admin/GetAdmins", function (res) {
        var tbody = $("#adminTbody").empty();

        if (!res.length) {
            tbody.append("<tr><td colspan='5' class='td-empty'>No admin accounts found.</td></tr>");
            return;
        }

        res.forEach(function (a) {
            var statusBadge = a.isActive
                ? "<span class='badge badge-active'>Active</span>"
                : "<span class='badge badge-inactive'>Inactive</span>";

            var toggleClass = a.isActive
                ? "btn-mini btn-toggle deactivate"
                : "btn-mini btn-toggle";
            var toggleLabel = a.isActive ? "Deactivate" : "Activate";

            tbody.append(
                "<tr>" +
                "<td>" + a.userID + "</td>" +
                "<td>" + a.username + "</td>" +
                "<td>" + a.createdAt + "</td>" +
                "<td>" + statusBadge + "</td>" +
                "<td>" +
                "<div class='action-container'>" +
                "<button class='" + toggleClass + "' onclick='toggleStatus(" + a.userID + ")'>" + toggleLabel + "</button>" +
                "<button class='btn-mini btn-reset' onclick='openReset(" + a.userID + ")'>Reset Password</button>" +
                "</div>" +
                "</td>" +
                "</tr>"
            );
        });
    });
}

function toggleStatus(userID) {
    if (!confirm("Toggle this admin's active status?")) return;
    $.post("/Admin/ToggleStatus", { userID: userID }, function (res) {
        if (res.mess === 1) loadAdmins();
        else showMsg("createMsg", "error", res.message);
    });
}

function openReset(userID) {
    $("#resetUserID").val(userID);
    $("#resetPassword, #resetConfirm").val("");
    $("#resetMsg").html("");
    $("#resetModal").addClass("active");
}

function closeModal() {
    $("#resetModal").removeClass("active");
}

function confirmReset() {
    var id = $("#resetUserID").val();
    var p = $("#resetPassword").val();
    var c = $("#resetConfirm").val();

    if (!p) { showModalMsg("error", "Please enter a new password."); return; }
    if (p !== c) { showModalMsg("error", "Passwords do not match."); return; }
    if (p.length < 6) { showModalMsg("error", "Password must be at least 6 characters."); return; }

    $.post("/Admin/ResetPassword", { userID: id, newPassword: p }, function (res) {
        if (res.mess === 1) {
            showModalMsg("success", res.message);
            setTimeout(function () { closeModal(); }, 1500);
        } else {
            showModalMsg("error", res.message);
        }
    });
}

function showMsg(id, type, msg) {
    var cls = type === "success" ? "msg-success" : "msg-error";
    $("#" + id).html("<p class='" + cls + "'>" + msg + "</p>");
}

function showModalMsg(type, msg) {
    var cls = type === "success" ? "msg-success" : "msg-error";
    $("#resetMsg").html("<p class='" + cls + "'>" + msg + "</p>");
}