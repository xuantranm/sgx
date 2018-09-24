$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.btn-edit').on('click', function () {
        $('.mode-read').addClass('d-none');
        $('.btn-list').removeClass('d-none');
        $('.mode-edit').removeClass('d-none');
        $('.btn-edit').addClass('d-none');
    });
    $('.btn-list').on('click', function () {
        $('.mode-read').removeClass('d-none');
        $('.btn-list').addClass('d-none');
        $('.mode-edit').addClass('d-none');
        $('.btn-edit').removeClass('d-none');
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
});


function registerAutoNumeric() {
    // No need set true value. When submit lib AutoNumeric auto generate true value.

    toithieuvung = new AutoNumeric('.toithieuvung', { decimalPlaces: 0 });
    toithieuvungdoanhnghiepapdung = new AutoNumeric('.toithieuvungdoanhnghiepapdung', { decimalPlaces: 0 });

    $('.codeHeSo').each(function (i, obj) {
        enableHeSo($(obj).val());
    });

    $('.codePhuCap').each(function (i, obj) {
        enablePhuCap($(obj).val());
    });

    $('.toithieuvungdoanhnghiepapdung').on('keyup', function () {
        var money = $(this).val();
        calculatorThangLuongMucToiThieu(money, 0, '');
    });
}

function enableHeSo(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.heso-' + code, { decimalPlaces: 2, allowDecimalPadding: false  });

    $('.heso-' + code).on('keyup', function () {
        var bac = $(this).val();
        var id = $(this).data('id');
        var money = $('.toithieuvungdoanhnghiepapdung').val();
        console.log(bac);
        calculatorThangLuongMucToiThieu(money,bac,id);
    });
}

function enablePhuCap(code) {
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

function calculatorThangLuongMucToiThieu(money, heso, id) {
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
                    console.log(value);
                    $('.money-' + value.id).html(accounting.formatNumber(value.money));
                    $('#heso-' + value.id).val(value.rate);
                });
            }
        });

    // set value
}