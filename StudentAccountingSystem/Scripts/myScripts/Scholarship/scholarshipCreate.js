$("#btnSave").click(function () {
    var name = $("#txtName").val().trim();
    var type = $("#selType").val();

    if (!name) {
        $("#result").html("<p class='msg-error'>Scholarship name is required.</p>");
        return;
    }
    if (!type) {
        $("#result").html("<p class='msg-error'>Please select a type.</p>");
        return;
    }

    var percent = $("#txtPercent").val();
    var amount = $("#txtAmount").val();

    $.post("/Scholarship/SaveScholarship", {
        name: name,
        type: type,
        percent: percent === "" ? "0" : percent,
        amount: amount === "" ? "0" : amount
    }, function (res) {
        if (res[0].mess == 1) {
            $("#result").html("<p class='msg-success'>" + res[0].message + " Redirecting...</p>");
            setTimeout(function () { window.location.href = "/Scholarship/Index"; }, 1500);
        } else {
            $("#result").html("<p class='msg-error'>" + res[0].message + "</p>");
        }
    }).fail(function (xhr) {
        $("#result").html("<p class='msg-error'>Request failed: " + xhr.status + " " + xhr.statusText + "</p>");
    });
});

$("#selType").change(function () {
    var val = $(this).val();
    $("#groupPercent").toggleClass("field-hidden", val !== "Percent");
    $("#groupAmount").toggleClass("field-hidden", val !== "Fixed");
});