$(document).ready(function () {
    loadDiscounts();

    $("#btnSearch").click(function () {
        var keyword = $("#txtSearch").val().toLowerCase();
        $("#tblBody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(keyword) > -1);
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
            html += "<tr>";
            html += "<td>" + (i + 1) + "</td>";
            html += "<td>" + res[i].name + "</td>";
            html += "<td>" + res[i].type + "</td>";
            html += "<td>" + displayVal + "</td>";
            html += "<td align='center'>" + res[i].enrolled + "</td>";
            html += "<td style='color:" + statusColor + "'><b>" + statusLabel + "</b></td>";
            html += "<td>" +
                "<input type='button' value='" + toggleLabel + "' onclick='toggleDiscount(" + res[i].id + ")' /> " +
                "<input type='button' value='Edit' onclick='editDiscount(" + res[i].id + ")' /> " +
                "<input type='button' value='Delete' onclick='deleteDiscount(" + res[i].id + ")' />" +
                "</td>";
            html += "</tr>";
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
    window.location.href = "/Discount/Edit/" + id;
}

function deleteDiscount(id) {
    if (confirm("Are you sure you want to delete this discount?")) {
        $.post("/Discount/DeleteDiscount", { id: id }, function (res) {
            if (res[0].mess == 1) {
                $("#msgBox").html("<span style='color:green'>" + res[0].message + "</span>");
                loadDiscounts();
            } else {
                $("#msgBox").html("<span style='color:red'>" + res[0].message + "</span>");
            }
        });
    }
}