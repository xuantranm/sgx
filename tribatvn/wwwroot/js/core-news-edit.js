$(function () {
    $('.content-text').summernote({
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

