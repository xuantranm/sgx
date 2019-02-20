$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    //$('.left-menu').addClass('d-none');
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#Thang').on('change', function (e) {
        formSubmit();
    });
});


