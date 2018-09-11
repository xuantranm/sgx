$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#cd').on('select2:select', function (e) {
        formSubmit();
    });
});