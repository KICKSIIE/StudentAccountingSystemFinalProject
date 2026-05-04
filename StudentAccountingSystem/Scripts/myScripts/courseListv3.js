$(document).ready(function () {
    loadCourses();

    $("#btnCancelCreate").click(function () {
        $("#divCreate").hide();
        $("#divList").show();
    });

    $("#btnSave").click(function () {
        var data = {
            code: $("#txtCode").val(),
            name: $("#txtName").val(),
            units: $("#txtUnits").val(),
            tpu: $("#txtTpu").val(),
            reg: $("#txtReg").val(),
            misc: $("#txtMisc").val(),
            lab: $("#txtLab").val()
        };

        $.post("/Course/SaveCourse", data, function (res) {
            alert(res.message);
            $("#divCreate").hide();
            $("#divList").show();
            loadCourses();
        }).fail(function () {
            alert("Error saving course.");
        });
    });
});

function loadCourses() {
    $.post("/Course/ViewAllCourses", {}, function (res) {
        var html = "";
        for (var i = 0; i < res.length; i++) {
            var statusLabel = res[i].isActive ? "Active" : "Inactive";
            var statusColor = res[i].isActive ? "green" : "red";
            var toggleLabel = res[i].isActive ? "Deactivate" : "Activate";

            html += "<tr>";
            html += "<td>" + (i + 1) + "</td>";
            html += "<td>" + res[i].code + "</td>";
            html += "<td>" + res[i].name + "</td>";
            html += "<td align='center'>" + res[i].enrolled + "</td>";
            html += "<td>" + res[i].units + "</td>";
            html += "<td>" + res[i].tpu + "</td>";
            html += "<td>" + res[i].reg + "</td>";
            html += "<td>" + res[i].misc + "</td>";
            html += "<td>" + res[i].lab + "</td>";
            html += "<td><b>" + res[i].totalFee + "</b></td>";
            html += "<td style='color:" + statusColor + "'><b>" + statusLabel + "</b></td>";
            html += "<td>" +
                "<input type='button' value='" + toggleLabel + "' onclick='toggleActive(" + res[i].id + ")' />" +
                " <input type='button' value='Edit' onclick='editCourse(" + res[i].id + ")' />" +
                " <input type='button' value='Delete' onclick='deleteCourse(" + res[i].id + ")' />" +
                "</td>";
            html += "</tr>";
        }
        $("#tblBody").html(html);
    });
}

function toggleActive(id) {
    $.post("/Course/ToggleCourseActive", { id: id }, function (res) {
        if (res[0].mess == 1) {
            $("#msgBox").html("<span style='color:green'>" + res[0].message + "</span>");
            loadCourses();
        } else {
            $("#msgBox").html("<span style='color:red'>" + res[0].message + "</span>");
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
                alert(res[0].message);
                loadCourses();
            } else {
                alert(res[0].message);
            }
        });
    }
}