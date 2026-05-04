$(document).ready(function () {
    loadCourses();

    $("#btnShowCreate").click(function () {
        $("#divList").hide();
        $("#divRegister").show();
    });

    $("#btnCancelRegister, #btnCancelEdit").click(function () {
        $("#divRegister, #divEdit").hide();
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
            $("#divRegister").hide();
            $("#divList").show();
            loadCourses();
        }).fail(function () {
            alert("Error saving course.");
        });
    });

    $("#btnUpdate").click(function () {
        var data = {
            courseID: $("#editCourseID").val(),
            code: $("#editCode").val(),
            name: $("#editName").val(),
            units: $("#editUnits").val(),
            tpu: $("#editTpu").val(),
            reg: $("#editReg").val(),
            misc: $("#editMisc").val(),
            lab: $("#editLab").val()
        };

        $.post("/Course/UpdateCourse", data, function (res) {
            alert(res.message);
            $("#divEdit").hide();
            $("#divList").show();
            loadCourses();
        });
    });
});

function loadCourses() {
    $.post("/Course/ViewAllCourses", {}, function (res) {
        console.log(res);
        var html = "";
        for (var i = 0; i < res.length; i++) {
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

            // STATUS COLUMN
            var statusLabel = res[i].isActive ? "Active" : "Inactive";
            var statusColor = res[i].isActive ? "green" : "red";
            html += "<td style='color:" + statusColor + "'><b>" + statusLabel + "</b></td>";

            // ACTIONS COLUMN — Toggle button is here, before Edit and Delete
            var toggleLabel = res[i].isActive ? "Deactivate" : "Activate";
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
    $.post("/Course/GetCourseByID", { id: id }, function (res) {
        $("#editCourseID").val(res.id);
        $("#editCode").val(res.code);
        $("#editName").val(res.name);
        $("#editUnits").val(res.units);
        $("#editTpu").val(res.tpu);
        $("#editReg").val(res.reg);
        $("#editMisc").val(res.misc);
        $("#editLab").val(res.lab);

        $("#divList, #divRegister").hide();
        $("#divEdit").show();
    });
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