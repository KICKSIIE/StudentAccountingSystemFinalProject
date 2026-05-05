$(document).ready(function () {
    loadCourses();

    $("#btnSearchCourse").click(function () {
        var keyword = $("#txtSearchCourse").val().toLowerCase();
        $("#tblBody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
        });
    });
});

function loadCourses() {
    $.post("/Course/ViewAllCourses", {}, function (res) {
        var html = "";

        if (res.length === 0) {
            html = "<tr><td colspan='12' class='td-empty'>No courses found.</td></tr>";
            $("#tblBody").html(html);
            return;
        }

        for (var i = 0; i < res.length; i++) {
            var isActive = res[i].isActive;
            var statusLabel = isActive ? "Active" : "Inactive";
            var statusClass = isActive ? "status-active" : "status-inactive";
            var toggleLabel = isActive ? "Deactivate" : "Activate";
            var toggleClass = isActive ? "btn-mini btn-toggle deactivate" : "btn-mini btn-toggle";

            html += "<tr>";
            html += "<td>" + (i + 1) + "</td>";
            html += "<td>" + res[i].code + "</td>";
            html += "<td>" + res[i].name + "</td>";
            html += "<td class='td-center'>" + res[i].enrolled + "</td>";
            html += "<td>" + res[i].units + "</td>";
            html += "<td>" + res[i].tpu + "</td>";
            html += "<td>" + res[i].reg + "</td>";
            html += "<td>" + res[i].misc + "</td>";
            html += "<td>" + res[i].lab + "</td>";
            html += "<td><b>" + res[i].totalFee + "</b></td>";
            html += "<td><span class='" + statusClass + "'>" + statusLabel + "</span></td>";
            html += "<td>";
            html += "<div class='action-container'>";
            html += "<button class='" + toggleClass + "' onclick='toggleActive(" + res[i].id + ")'>" + toggleLabel + "</button>";
            html += "<button class='btn-mini btn-edit' onclick='editCourse(" + res[i].id + ")'>Edit</button>";
            html += "<button class='btn-mini btn-delete' onclick='deleteCourse(" + res[i].id + ")'>Delete</button>";
            html += "</div>";
            html += "</td>";
            html += "</tr>";
        }

        $("#tblBody").html(html);
    });
}

function toggleActive(id) {
    $.post("/Course/ToggleCourseActive", { id: id }, function (res) {
        if (res[0].mess == 1) {
            $("#msgBox").html("<span class='msg-success'>" + res[0].message + "</span>");
            loadCourses();
        } else {
            $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
        }
    });
}

function editCourse(id) {
    window.location.href = "/Course/Edit/" + id;
}

function deleteCourse(id) {
    if (confirm("Are you sure you want to delete this course?")) {
        $.post("/Course/DeleteCourse", { id: id }, function (res) {
            if (res[0].mess == 1) {
                $("#msgBox").html("<span class='msg-success'>" + res[0].message + "</span>");
                loadCourses();
            } else {
                $("#msgBox").html("<span class='msg-error'>" + res[0].message + "</span>");
            }
        });
    }
}