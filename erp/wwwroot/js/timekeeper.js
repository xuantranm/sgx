$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    //$('#times').on('select2:select', function (e) {
    //    formSubmit();
    //});

    $('ddl-times').on('change', function (e) {
        formSubmit();
    });

    $('#requestTimeKeeperModal').on('show.bs.modal', function (event) {
        var a = $(event.relatedTarget); // Button that triggered the modal
        var recipient = a.data('id'); // Extract info from data-* attributes
        var url = $('#hidUrlGetItem').val();
        var modal = $(this);
        $.ajax({
            type: "post",
            url: url,
            data: { id: recipient },
            success: function (data) {
                if (data.length !== 0) {
                    modal.find('.data-item-edit').html($.templates("#tmplDataItem").render(data));
                }
                $('textarea.js-auto-size').textareaAutoSize();
            }
        });
    });

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        var $this = $(this);
        var frmValues = $this.serialize();
        console.log(frmValues);

        // loading button
        $('.btn-submit-timekeeping').prop('disabled', true);
        //$('input', $('.data-form')).prop('disabled', true);
        //$('select', $('.data-form')).prop('disabled', true);
        //$('textarea', $('.data-form')).prop('disabled', true);

        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-submit-timekeeping').html(loadingText);
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
                    $('.btn-submit-timekeeping').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    $('select', $('.data-form')).prop('disabled', false);
                    $('textarea', $('.data-form')).prop('disabled', false);
                    $('.btn-submit-timekeeping').html($('.btn-submit-timekeeping').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Có lỗi xảy ra. Liên hệ hỗ trợ hotro@tribat.vn");
            });
    });
});