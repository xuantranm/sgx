$(function () {
    $('#nhom').on('change', function () {
        loadDataByNhom();
    });

    $('#phannhom').on('change', function () {
        loadDataByPhanNhom();
    });

    $('#Content').summernote({
        placeholder: 'Chi tiết sản phẩm',
        tabsize: 2,
        minHeight: 300,
        popover: {
            image: [],
            link: [],
            air: []
        }
    });

    //$('#summernote').summernote({
    //    height: 300,                 // set editor height
    //    minHeight: null,             // set minimum height of editor
    //    maxHeight: null,             // set maximum height of editor
    //    focus: true                  // set focus to editable area after initializing summernote
    //});

    

    document.getElementById('files').addEventListener('change', handleFileSelect, false);

    $('.btn-seo').on('click', function () {
        if ($('.seo').hasClass('d-none')) {
            $('.btn-seo').text('Không SEO');
            $('.seo').removeClass('d-none');
            $('#KeyWords').focus();
        }
        else {
            $('.btn-seo').text('Thêm SEO');
            $('.seo').addClass('d-none');
        }
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