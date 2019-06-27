$(function () {
    $('.page-click').on('click', function () {
        var parentForm = $(this).closest('form');
        $('#trang-fpage', parentForm).val($(this).attr("data-page"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('#TrangDll').on('change', function () {
        var parentForm = $(this).closest('form');
        $('#trang-fpage', parentForm).val($(this).val());
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.btn-sort').on('click', function () {
        var parentForm = $(this).closest('form');
        $('.sap-xep', parentForm).val($(this).attr("data-sortby"));
        $('.thu-tu', parentForm).val($(this).attr("data-sortorder"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.from-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.tu-ngay').val(date.format('MM-DD-YYYY'));
        formSubmit();
    });

    $('.to-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.den-ngay').val(date.format('MM-DD-YYYY'));
        formSubmit();
    });
});