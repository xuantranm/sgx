$(function () {
    $('.left-menu').addClass('d-none');

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    registerAutoNumeric();

    $(".data-form").on("submit", function (event) {
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
                    //$('select', $('.data-form')).prop('disabled', false);
                    //$('textarea', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });

    function registerAutoNumeric() {
        $('.numeric').each(function (i, obj) {
            enableNumeric($(obj));
        });
    }

    function enableNumeric(element) {
        var code = $(element).data('id');
        newInstall = new AutoNumeric('.' + code, { decimalPlaces: 0 });
        $('.' + code).on('keyup', function () {
            calculatorData(code.split('-')[1]);
        });
    }

    function calculatorData(code) {
        var chitieudoanhso = Math.round($('.chitieudoanhso-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var chitieudoanhthu = Math.round($('.chitieudoanhthu-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var chitieudophu = Math.round($('.chitieudophu-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var chitieumomoi = Math.round($('.chitieumomoi-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var chitieunganhhang = Math.round($('.chitieunganhhang-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));

        var thuchiendoanhso = Math.round($('.thuchiendoanhso-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var thuchiendoanhthu = Math.round($('.thuchiendoanhthu-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var thuchiendophu = Math.round($('.thuchiendophu-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var thuchienmomoi = Math.round($('.thuchienmomoi-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var thuchiennganhhang = Math.round($('.thuchiennganhhang-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        
        //$('.luongcbbaogomphucap-' + code).html(accounting.formatNumber(luongcbbaogomphucap));
        // ajax get
        $.ajax({
            type: "post",
            url: $('#hidCalculatorDataKPI').val(),
            data: {
                id: $('.tr-' + code).data('id'),
                chitieudoanhso,
                chitieudoanhthu,
                chitieudophu,
                chitieumomoi,
                chitieunganhhang,
                thuchiendoanhso,
                thuchiendoanhthu,
                thuchiendophu,
                thuchienmomoi,
                thuchiennganhhang
            },
            success: function (data) {
                console.log(data);
                if (data.result === true) {
                    //$('.leave-duration').text(data.date);
                }
                else {
                    //$('.leave-duration').text(data.message);
                }
            }
        });

        //var tongthunhap = luongcbbaogomphucap + 0;
        //$('.tongthunhap-' + code).html(accounting.formatNumber(tongthunhap));
        //// ajax get
        //var thuclanh = tongthunhap;
        //$('.thuclanh-' + code).html(accounting.formatNumber(thuclanh));
    }
});


