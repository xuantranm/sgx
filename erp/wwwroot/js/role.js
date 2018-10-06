$(function () {
    $('#editItemModal').on('show.bs.modal', function (event) {
        var a = $(event.relatedTarget); // Button that triggered the modal
        var recipient = a.data('id'); // Extract info from data-* attributes
        // If necessary, you could initiate an AJAX request here (and then do the updating in a callback).
        // Update the modal's content. We'll use jQuery here, but you could use a data binding library or other methods instead.
        var modal = $(this);
        $.ajax({
            type: "GET",
            url: $('#hidDetailLink').val(),
            contentType: "application/json; charset=utf-8",
            data: { id: recipient },
            dataType: "json",
            success: function (data) {
                //console.log(data);
                if (data.result === false) {
                    console.log("Error.");
                    console.log(data);
                }
                else {
                    if (data.length !== 0) {
                        modal.find('.data-item-edit').html($.templates("#tmplDataItem").render(data));
                    }
                }
            }
        });
    });

    $('#disableItemModal').on('show.bs.modal', function (event) {
        var a = $(event.relatedTarget); // Button that triggered the modal
        var recipient = a.data('id'); // Extract info from data-* attributes
        // If necessary, you could initiate an AJAX request here (and then do the updating in a callback).
        // Update the modal's content. We'll use jQuery here, but you could use a data binding library or other methods instead.
        var modal = $(this);
        $.ajax({
            type: "GET",
            url: $('#hidDetailLink').val(),
            contentType: "application/json; charset=utf-8",
            data: { id: recipient },
            dataType: "json",
            success: function (data) {
                //console.log(data);
                if (data.result === false) {
                    console.log("Error.");
                    console.log(data);
                }
                else {
                    if (data.length !== 0) {
                        modal.find('.data-item-disable').html($.templates("#tmplDataItemPlainText").render(data));
                    }
                }
            }
        });
    });

    $('#activeItemModal').on('show.bs.modal', function (event) {
        var a = $(event.relatedTarget); // Button that triggered the modal
        var recipient = a.data('id'); // Extract info from data-* attributes
        // If necessary, you could initiate an AJAX request here (and then do the updating in a callback).
        // Update the modal's content. We'll use jQuery here, but you could use a data binding library or other methods instead.
        var modal = $(this);
        $.ajax({
            type: "GET",
            url: $('#hidDetailLink').val(),
            contentType: "application/json; charset=utf-8",
            data: { id: recipient },
            dataType: "json",
            success: function (data) {
                //console.log(data);
                if (data.result === false) {
                    console.log("Error.");
                    console.log(data);
                }
                else {
                    if (data.length !== 0) {
                        modal.find('.data-item-active').html($.templates("#tmplDataItemPlainText").render(data));
                    }
                }
            }
        });
    });

    $('#deleteItemModal').on('show.bs.modal', function (event) {
        var a = $(event.relatedTarget); // Button that triggered the modal
        var recipient = a.data('id'); // Extract info from data-* attributes
        // If necessary, you could initiate an AJAX request here (and then do the updating in a callback).
        // Update the modal's content. We'll use jQuery here, but you could use a data binding library or other methods instead.
        var modal = $(this);
        $.ajax({
            type: "GET",
            url: $('#hidDetailLink').val(),
            contentType: "application/json; charset=utf-8",
            data: { id: recipient },
            dataType: "json",
            success: function (data) {
                //console.log(data);
                if (data.result === false) {
                    console.log("Error.");
                    console.log(data);
                }
                else {
                    if (data.length !== 0) {
                        modal.find('.data-item-delete').html($.templates("#tmplDataItemPlainText").render(data));
                    }
                }
            }
        });
    });

    $(".data-form").on("submit", function (event) {
        var $this = $(this);
        var frmValues = $this.serialize();
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            data: frmValues
        })
            .done(function (data) {
                toastr.info(data.message);
                //window.location.reload();
                setTimeout(function () {
                    window.location.reload();
                }, 1000);
            })
            .fail(function () {
                toastr.error(data.message);
            });
        event.preventDefault();
    });
});