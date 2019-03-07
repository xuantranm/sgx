$(function () {
    $('.btn-language').on('click', function () {
        var language = $(this).attr("data-value");
        //Set cookie
        $.removeCookie('language', { path: '/' });
        $.cookie('language', language, { expires: 7, path: '/' });
        var url = $('.' + language, $('.link-languages')).data('value');
        $.ajax({
            url: '/language/change/',
            data: { language: language },
            type: 'post',
            dataType: 'json',
            success: function (data) {
                if (data === true) {
                    //window.location.reload();
                    console.log(url);
                    if (typeof url !== "undefined") {
                        window.location.replace(url);
                    }
                    else {
                        window.location.reload();
                    }
                }
            }
        });
    });

    $('textarea.js-auto-size').textareaAutoSize();

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