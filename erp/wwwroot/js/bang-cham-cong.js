$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.left-menu').addClass('d-none');

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlphong').on('change', function () {
        formSubmit();
    });

    $('.ddlEmployeeId').on('change', function () {
        formSubmit();
    });

    $('#thang').on('change', function (e) {
        formSubmit();
    });

    $('.from-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.tu-ngay').val(
            date.format('MM-DD-YYYY')
        );
    });

    $('.to-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.den-ngay').val(
            date.format('MM-DD-YYYY')
        );
    });
});


