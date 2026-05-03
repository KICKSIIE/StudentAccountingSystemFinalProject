$(document).ready(function () {
    loadScholarships();

    // Toggle Percent/Fixed fields
    $("#selType").change(function () {
        var val = $(this).val();
        $("#groupPercent").toggle(val === "Percent");
        $("#groupAmount").toggle(val === "Fixed");
    });

    $("#btnShowCreate").click(function () {
        $("#formTitle").text("Add New Scholarship");
        $("#hdnID, #txtName, #txtPercent, #txtAmount").val("");
        $("#selType").val("").change();
        $("#divList").hide();
        $("#divForm").show();
    });

    $("#btnCancel").click(function () {
        $("#divForm").hide();
        $("#divList").show();
    });

    $("#btnSave").click(function () {
        var id = $("#hdnID").val();
        var url = id ? "/Scholarship/UpdateScholarship" : "/Scholarship/SaveScholarship";

        var data = {
            id: id,
            name: $("#txtName").val(),
            type: $("#selType").val(),
            percent: $("#txtPercent").val(),
            amount: $("#txtAmount").val()
        };

        $.post(url, data, function (res) {
            alert(res[0].message);
            if (res[0].mess == 1) {
                $("#divForm").hide();
                $("#divList").show();
                loadScholarships();
            }
        });
    });
});

function loadScholarships() {
    $.post("/Scholarship/ViewAllScholarships", {}, function (res) {
        var html = "";
        for (var i = 0; i < res.length; i++) {
            var displayVal = res[i].type === "Percent" ? res[i].percent + "%" : "₱" + res[i].amount;
            html += "<tr>" +
                "<td>" + (i + 1) + "</td>" +
                "<td>" + res[i].name + "</td>" +
                "<td>" + res[i].type + "</td>" +
                "<td>" + displayVal + "</td>" +
                "<td>" + res[i].enrolled + "</td>" +
                "<td>" +
                "<input type='button' value='Edit' onclick='editSch(" + res[i].id + ")' /> " +
                "<input type='button' value='Delete' onclick='deleteSch(" + res[i].id + ")' />" +
                "</td>" +
                "</tr>";
        }
        $("#tblBody").html(html);
    });
}

function editSch(id) {
    $.post("/Scholarship/GetScholarshipByID", { id: id }, function (res) {
        $("#formTitle").text("Edit Scholarship");
        $("#hdnID").val(res.id);
        $("#txtName").val(res.name);
        $("#selType").val(res.type).change();
        $("#txtPercent").val(res.percent);
        $("#txtAmount").val(res.amount);
        $("#divList").hide();
        $("#divForm").show();
    });
}

function deleteSch(id) {
    if (confirm("Delete this scholarship?")) {
        $.post("/Scholarship/DeleteScholarship", { id: id }, function (res) {
            alert(res[0].message);
            if (res[0].mess == 1) loadScholarships();
        });
    }
}