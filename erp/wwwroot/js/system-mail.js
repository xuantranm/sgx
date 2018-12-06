$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('#status').on('change', function (e) {
        formSubmit();
    });

    $('#size').on('change', function (e) {
        formSubmit();
    });

    $('#page').on('change', function (e) {
        formSubmit();
    });

    $('#emailModal').on('show.bs.modal', function (event) {
        var a = $(event.relatedTarget); // Button that triggered the modal
        var recipient = a.data('id'); // Extract info from data-* attributes
        $('#hidId').val(recipient);
        var url = $('#hidUrlGetItem').val();
        var modal = $(this);
        $.ajax({
            type: "post",
            url: url,
            data: { id: recipient },
            success: function (data) {
                console.log(data);
                if (data.length !== 0) {
                    modal.find('.content-email-modal').html(data);
                }
            }
        });
    });

    $(".data-form").on("submit", function (event) {
        //if (!$('input[type=checkbox]:checked').length) {
        //    toastr.error("Chọn email để gửi lại.");
        //    //stop the form from submitting
        //    return false;
        //}

        event.preventDefault();
        var $this = $(this);
        var frmValues = $this.serialize();

        // loading button
        $('.btn-submit').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-submit').html(loadingText);

        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            data: frmValues
        })
            .done(function (data) {
                if (data.result === true) {
                    toastr.success(data.message);
                    setTimeout(function () {
                        window.location.reload();
                    }, 1000);
                }
                else {
                    toastr.error(data.message);
                    $('.btn-submit').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });

});

