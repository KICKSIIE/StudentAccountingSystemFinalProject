$(document).ready(function () {
    loadScholarships();

    $("#btnSearch").click(function () {
        var keyword = $("#txtSearch").val().toLowerCase();
        $("#tblBody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });
});

function loadScholarships() {
    $.post("/Scholarship/ViewAllScholarships", {}, function (res) {
        var html = "";

        if (res.length === 0) {
            html = "<tr><td colspan='7' class='td-empty'>No scholarships found.</td></tr>";
            $("#tblBody").html(html);
            return;
        }

        for (var i = 0; i < res.length; i++) {
            var displayVal = res[i].type === "Percent" ? res[i].percent + "%" : "₱" + res[i].amount;
            var statusLabel = res[i].isActive ? "Active" : "Inactive";
            var statusClass = res[i].isActive ? "status-active" : "status-inactive";
            var toggleLabel = res[i].isActive ? "Deactivate" : "Activate";
            var toggleClass = res[i].isActive ? "btn-mini btn-toggle deactivate" : "btn-mini btn-toggle";

            html += "<tr>";
            html += "<td>" + (i + 1) + "</td>";
            html += "<td>" + res[i].name + "</td>";
            html += "<td>" + res[i].type + "</td>";
            html += "<td>" + displayVal + "</td>";
            html += "<td class='td-center'>" + res[i].enrolled + "</td>";
            html += "<td><span class='" + statusClass + "'>" + statusLabel + "</span></td>";
            html += "<td>";
            html += "<div class='action-container'>";
            html += "<button class='" + toggleClass + "' onclick='toggleScholarship(" + res[i].id + ")'>" + toggleLabel + "</button>";
            html += "<button class='btn-mini btn-edit' onclick='editSch(" + res[i].id + ")'>Edit</button>";
            html += "<button class='btn-mini btn-delete' onclick='deleteSch(" + res[i].id + ")'>Delete</button>";
            html += "</div>";
            html += "</td>";
            html += "</tr>";
        }

        $("#tblBody").html(html);
    });
}

function toggleScholarship(id) {
    $.post("/Scholarship/ToggleScholarshipActive", { id: id }, function (res) {
        if (res[0].mess == 1) {
            $("#msgBox").html("<span class='msg-success'>" + res[0].message + "</span>");
            loadScholarships();
        } else {
            $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
        }
    });
}

function editSch(id) {
    window.location.href = "/Scholarship/Edit/" + id;
}

function deleteSch(id) {
    if (confirm("Delete this scholarship?")) {
        $.post("/Scholarship/DeleteScholarship", { id: id }, function (res) {
            if (res[0].mess == 1) {
                $("#msgBox").html("<span class='msg-success'>" + res[0].message + "</span>");
                loadScholarships();
            } else {
                $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
            }
        });
    }
}