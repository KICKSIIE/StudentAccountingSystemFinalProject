$(document).ready(function () {
    loadDiscounts();

    // Toggle fields based on Type selection
    $("#selType").change(function () {
        var val = $(this).val();
        $("#groupPercent").toggle(val === "Percent");
        $("#groupAmount").toggle(val === "Fixed");
    });

    $("#btnShowCreate").click(function () {
        $("#formTitle").text("Add New Discount");
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
        var url = id ? "/Discount/UpdateDiscount" : "/Discount/SaveDiscount";

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
                loadDiscounts();
            }
        });
    });
});

function loadDiscounts() {
    $.post("/Discount/ViewAllDiscounts", {}, function (res) {
        var html = "";
        for (var i = 0; i < res.length; i++) {
            var displayVal = res[i].type === "Percent" ? res[i].percent + "%" : "₱" + res[i].amount;
            var statusLabel = res[i].isActive ? "Active" : "Inactive";
            var statusColor = res[i].isActive ? "green" : "red";
            var toggleLabel = res[i].isActive ? "Deactivate" : "Activate";

            html += "<tr>" +
                "<td>" + (i + 1) + "</td>" +
                "<td>" + res[i].name + "</td>" +
                "<td>" + res[i].type + "</td>" +
                "<td>" + displayVal + "</td>" +
                "<td align='center'>" + res[i].enrolled + "</td>" +
                "<td style='color:" + statusColor + "'><b>" + statusLabel + "</b></td>" +
                "<td>" +
                "<input type='button' value='" + toggleLabel + "' onclick='toggleDiscount(" + res[i].id + ")' /> " +
                "<input type='button' value='Edit' onclick='editDiscount(" + res[i].id + ")' /> " +
                "<input type='button' value='Delete' onclick='deleteDiscount(" + res[i].id + ")' />" +
                "</td>" +
                "</tr>";
        }
        $("#tblBody").html(html);
    });
}

function toggleDiscount(id) {
    $.post("/Discount/ToggleDiscountActive", { id: id }, function (res) {
        if (res[0].mess == 1) {
            $("#msgBox").html("<span style='color:green'>" + res[0].message + "</span>");
            loadDiscounts();
        } else {
            $("#msgBox").html("<span style='color:red'>" + res[0].message + "</span>");
        }
    });
}
function editDiscount(id) {
    $.post("/Discount/GetDiscountByID", { id: id }, function (res) {
        $("#formTitle").text("Edit Discount");
        $("#hdnID").val(res.id);
        $("#txtName").val(res.name);
        $("#selType").val(res.type).change();
        $("#txtPercent").val(res.percent);
        $("#txtAmount").val(res.amount);
        $("#divList").hide();
        $("#divForm").show();
    });
}

function deleteDiscount(id) {
    if (confirm("Are you sure you want to delete this discount?")) {
        $.post("/Discount/DeleteDiscount", { id: id }, function (res) {
            alert(res[0].message);
            if (res[0].mess == 1) loadDiscounts();
        });
    }
}