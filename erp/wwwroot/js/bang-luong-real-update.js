$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.left-menu').addClass('d-none');

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#thang').on('change', function (e) {
        formSubmit();
    });

    registerAutoNumeric();

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        var $this = $(this);
        var frmValues = $this.serialize();
        console.log(frmValues);
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
                console.log(data);
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
        var dataInput = $('.tr-' + code + ' :input').serialize().replace(new RegExp('%5B' + code + '%5D', 'g'), '%5B0%5D');
        $.ajax({
            type: "post",
            url: $('#hidCalculatorLuong').val(),
            data: dataInput,
            success: function (data) {
                console.log(data);
                $('.luongcbbaogomphucap-' + code).html(accounting.formatNumber(data.entity.luongCoBanBaoGomPhuCap / 1000));
                $('.tongthunhap-' + code).html(accounting.formatNumber(data.entity.tongThuNhap / 1000));
                $('.tongthunhapphut-' + code).html(accounting.formatNumber(data.entity.tongThuNhapPhut / 1000));
                $('.bHXHBHYT-' + code).html(accounting.formatNumber(data.entity.bhxhbhyt / 1000));
                $('.thuclanh-' + code).html(accounting.formatNumber(data.entity.thucLanh / 1000));
                $('.thuclanhphut-' + code).html(accounting.formatNumber(data.entity.thucLanhPhut / 1000));
            }
        });
    }
});


