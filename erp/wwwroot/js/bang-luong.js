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

    $('#Thang').on('change', function (e) {
        formSubmit();
    });

    $('#PhongBan').on('change', function (e) {
        formSubmit();
    });

    $('#Id').on('change', function (e) {
        formSubmit();
    });
});