$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#Thang').on('change', function (e) {
        formSubmit();
    });

    $('#Id').on('change', function (e) {
        formSubmit();
    });

    $('#phongban').on('change', function (e) {
        formSubmit();
    });
});