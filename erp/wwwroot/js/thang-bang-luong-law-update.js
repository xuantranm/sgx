$(function () {
    $('.left-menu').addClass('d-none');

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    registerAutoNumericThangBangLuongLaw();

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
});

function registerAutoNumericThangBangLuongLaw() {
    // No need set true value. When submit lib AutoNumeric auto generate true value.
    toithieuvung = new AutoNumeric('.toithieuvung', { decimalPlaces: 0 });
    toithieuvungdoanhnghiepapdung = new AutoNumeric('.toithieuvungdoanhnghiepapdung', { decimalPlaces: 0 });

    $('.codeHeSo').each(function (i, obj) {
        enableHeSoLaw($(obj).val());
    });

    $('.codeMucLuong').each(function (i, obj) {
        enableMucLuongLaw($(obj).val(), $(obj).data('code'));
    });

    $('.codePhuCap').each(function (i, obj) {
        enablePhuCapLaw($(obj).val());
    });

    $('.toithieuvungdoanhnghiepapdung').on('keyup', function () {
        var money = $(this).val();
        calculatorThangLuongMucToiThieuLaw(money, 0, '');
    });
}

function enableHeSoLaw(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.heso-' + code, { decimalPlaces: 2, allowDecimalPadding: false  });

    $('.heso-' + code).on('keyup', function () {
        var bac = $(this).val();
        var id = $(this).data('id');
        var money = $('.mucluong-'+code).val();
        calculatorThangLuongMucToiThieuLaw(money,bac,id);
    });
}

function enableMucLuongLaw(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.mucluong-' + code, { decimalPlaces: 0 });

    $('.mucluong-' + code).on('keyup', function () {
        // change text in same tr.
        var heso = 1;
        var id = $(this).data('id');
        var money = $(this).val();
        calculatorThangLuongMucToiThieuLaw(money, heso, id);
    });
}

function enablePhuCapLaw(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.phucap-' + code, { decimalPlaces: 0 });
    //$('.phucap-' + code).on('keyup', function () {
    //    var bac = $(this).val();
    //    var id = $(this).data('id');
    //    var money = $('.toithieuvungdoanhnghiepapdung').val();
    //    console.log(bac);
    //    calculatorThangLuongMucToiThieu(money, bac, id);
    //});
}

function calculatorThangLuongMucToiThieuLaw(money, heso, id) {
    // Get ajax calculator
    $.ajax({
        type: 'get',
        url: $('#hidCalculatorThangBangLuongLaw').val(),
        data: {
            money: money,
            heso: heso,
            id: id
        }
    })
        .done(function (data) {
            console.log(data);
            if (data.result === true) {
                $.each(data.list, function (index, value) {
                    $('.money-' + value.id).html(accounting.formatNumber(value.money));
                    // Furure check id he so, change => calculator again.
                });
            }
        });

    // set value
}