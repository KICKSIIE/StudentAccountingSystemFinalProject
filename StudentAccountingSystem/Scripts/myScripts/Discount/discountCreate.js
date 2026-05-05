$("#selType").change(function () {
    var val = $(this).val();
    $("#groupPercent").toggle(val === "Percent");
    $("#groupAmount").toggle(val === "Fixed");
});

$("#btnSave").click(function () {
    var name = $("#txtName").val().trim();
    var type = $("#selType").val();

    if (!name) {
        $("#result").html("<p class='msg-error'>Discount name is required.</p>");
        return;
    }
    if (!type) {
        $("#result").html("<p class='msg-error'>Please select a type.</p>");
        return;
    }

    var percent = $("#txtPercent").val();
    var amount = $("#txtAmount").val();

    $.post("/Discount/SaveDiscount", {
        name: name,
        type: type,
        percent: percent === "" ? "0" : percent,
        amount: amount === "" ? "0" : amount
    }, function (res) {
        if (res[0].mess == 1) {
            $("#result").html("<p class='msg-success'>" + res[0].message + " Redirecting...</p>");
            setTimeout(function () { window.location.href = "/Discount/Index"; }, 1500);
        } else {
            $("#result").html("<p class='msg-error'>" + res[0].message + "</p>");
        }
    }).fail(function (xhr) {
        $("#result").html("<p class='msg-error'>Request failed: " + xhr.status + " " + xhr.statusText + "</p>");
    });
});