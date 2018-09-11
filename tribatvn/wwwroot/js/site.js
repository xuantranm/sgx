$(function () {
    $('textarea.js-auto-size').textareaAutoSize();

    $(document).ready(function () {
        fixImg();
        $(window).on('resize', function () {
            fixImg();
        });
    });

    function fixImg() {
        if ($(window).width() < 575.98) {
            $('.content-body img').css({ 'width': '100%' });
        }
    }
});