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

function handleFileSelect(evt) {
    var files = evt.target.files; // FileList object

    // Loop through the FileList and render image files as thumbnails.
    for (var i = 0, f; f = files[i]; i++) {

        // Only process image files.
        if (!f.type.match('image.*')) {
            continue;
        }

        var reader = new FileReader();

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {
                // Render thumbnail.
                var span = document.createElement('span');
                span.innerHTML = ['<img class="thumb" src="', e.target.result,
                    '" title="', escape(theFile.name), '"/>'].join('');
                document.getElementById('list').insertBefore(span, null);
            };
        })(f);

        // Read in the image file as a data URL.
        reader.readAsDataURL(f);
    }
}

//function loadLogProduct(code) {
//    $.ajax({
//        url: '/helper/logssanpham',
//        type: 'GET',
//        data: { code: code },
//        datatype: 'json',
//        contentType: 'application/json; charset=utf-8',
//        success: function (data) {
//            if (data.length !== 0) {
//                var tmpl = $.templates("#logTmpl");
//                var html = tmpl.render(data.logs);
//                $('.loglist').html(html);
//            }
//        }
//    });
//}