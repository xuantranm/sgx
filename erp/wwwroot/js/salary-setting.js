$(function () {
    //$('.left-menu').addClass('d-none');
    $('.btn-edit').on('click', function () {
        $('.btn-edit').addClass('d-none');
        $('.btn-submit').removeClass('d-none');
        $('.btn-back').removeClass('d-none');
        $('.mode-read').addClass('d-none');
        $('.mode-edit').removeClass('d-none');
    });


    $('.btn-back').on('click', function () {
        $('.btn-edit').removeClass('d-none');
        $('.btn-submit').addClass('d-none');
        $('.btn-back').addClass('d-none');
        $('.mode-read').removeClass('d-none');
        $('.mode-edit').addClass('d-none');
    });

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    autoNumericSalarySetting();

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        var $this = $(this);
        var frmValues = $this.serialize();

        // loading button
        $('.btn-submit').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        $('textarea', $('.data-form')).prop('disabled', true);
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
                    $('textarea', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });

    function autoNumericSalarySetting() {
        $('.codeNumeric').each(function (i, obj) {
            enableNum($(obj).val());
        });
    }

    function enableNum(code) {
        var newInstall = code;
        newInstall = new AutoNumeric('.numeric-' + code, { decimalPlaces: 0 });
    }
});

