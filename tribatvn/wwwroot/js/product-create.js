$(function () {
    $('.content-text').summernote({
        placeholder: 'Chi tiết sản phẩm',
        tabsize: 2,
        minHeight: 300,
        popover: {
            image: [],
            link: [],
            air: []
        }
    });

    document.getElementById('files').addEventListener('change', handleFileSelect, false);

    $('.vi-input').blur(function () {
        var ob = $(this).attr('data-element');
        $.ajax({
            type: "POST",
            url: "/api/translate/en/" + $(this).val(),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                console.log(data);
                $('.en-' + ob).val(data);
            }
        });
    });
});