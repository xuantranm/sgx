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
        $('.btnSubmitThangBangLuongLaw').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btnSubmitThangBangLuongLaw').html(loadingText);

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
                    $('.btnSubmitThangBangLuongLaw').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    //$('select', $('.data-form')).prop('disabled', false);
                    //$('textarea', $('.data-form')).prop('disabled', false);
                    $('.btnSubmitThangBangLuongLaw').html($('.btnSubmitThangBangLuongLaw').data('original-text'));
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
            calculatorLuong(code.split('-')[1]);
        });
    }
    
    function calculatorLuong(code) {
        var luongcb = 0;
        var dochai = Math.round($('.dochai-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var trachnhiem = Math.round($('.trachnhiem-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var thamnien = 0;
        var thuhut = Math.round($('.thuhut-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var xang = Math.round($('.xang-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var dienthoai = Math.round($('.dienthoai-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var com = Math.round($('.com-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var kiemnhiem = Math.round($('.kiemnhiem-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var bhytdacbiet = Math.round($('.bhytdacbiet-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var vitricanknnhieunam = Math.round($('.vitricanknnhieunam-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var vitridacthu = Math.round($('.vitridacthu-' + code).val().replace(/\D(\d{2})$/, '.$1').replace(/[^\d.]+/g, ""));
        var luongcbbaogomphucap = luongcb + dochai + trachnhiem + thamnien + thuhut + xang + dienthoai + com + kiemnhiem + bhytdacbiet + vitricanknnhieunam + vitridacthu;
        //console.log(input);
        $('.luongcbbaogomphucap-' + code).html(accounting.formatNumber(luongcbbaogomphucap));
        // ajax get
        $.ajax({
            type: "post",
            url: $('#hidCalculatorTongThuNhap').val(),
            data: {
                id: $('.tr-'+code).data('id'),
                to: toPost,
                scheduleWorkingTime: $('#Leave_WorkingScheduleTime').val(),
                type: $('#Leave_TypeId').val()
            },
            success: function (data) {
                console.log(data);
                if (data.result === true) {
                    $('.leave-duration').text(data.date);
                }
                else {
                    $('.leave-duration').text(data.message);
                }
            }
        });

        var tongthunhap = luongcbbaogomphucap + 0;
        $('.tongthunhap-' + code).html(accounting.formatNumber(tongthunhap));
        // ajax get
        var thuclanh = tongthunhap;
        $('.thuclanh-' + code).html(accounting.formatNumber(thuclanh));
    }
});


