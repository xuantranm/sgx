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

        $('.btn-submit').prop('disabled', true);
        $('.btn-back').prop('disabled', true);
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
                    $('.btn-back').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });
});


function registerAutoNumeric() {
    // No need set true value. When submit lib AutoNumeric auto generate true value.
    toithieuvungdoanhnghiepapdung = new AutoNumeric('.toithieuvungdoanhnghiepapdung', { decimalPlaces: 0 });

    $('.codeHeSo').each(function (i, obj) {
        enableHeSoReal($(obj).val());
    });

    $('.codeMucLuong').each(function (i, obj) {
        enableMucLuongReal($(obj).val());
    });

    $('.toithieuvungdoanhnghiepapdung').on('keyup', function () {
        var money = $(this).val();
        //update diem tham khao
        $('.mucluongthamkhao').each(function (index, currentElement) {
            if (parseInt($(currentElement).val().replace(/,/g, '')) < parseInt(money.replace(/,/g, ''))) {
                $(currentElement).val(money);
            }
        });
        calculatorThangLuong('', 0, money);
    });
}

function enableHeSoReal(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.heso-' + code, { decimalPlaces: 2, allowDecimalPadding: false  });

    $('.heso-' + code).on('keyup', function () {
        var heso = $(this).val();
        var id = $('.id-'+ code).val();
        var money = $('.mucluong-' + code).val();
        calculatorThangLuong(id, heso, money);
    });
}

function enableMucLuongReal(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.mucluong-' + code, { decimalPlaces: 2, allowDecimalPadding: false });

    $('.mucluong-' + code).on('keyup', function () {
        var heso = $('.heso-'+ code).val();
        var id = $('.id-' + code).val();
        var money = $(this).val();
        calculatorThangLuong(id, heso, money);
    });
}

function calculatorThangLuong(id, heso, money) {
    $.ajax({
        type: 'get',
        url: $('#hidCalculatorThangBangLuongReal').val(),
        data: {
            thang: $('#thang').val(),
            id: id,
            heso: heso,
            money: money
        }
    })
        .done(function (data) {
            if (data.result === true) {
                $.each(data.list, function (index, value) {
                    $('.mucluongresult-' + value.id).html(accounting.formatNumber(value.money));
                });
            }
        });
}