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