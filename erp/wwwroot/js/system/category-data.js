$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddl-type').on('change', function () {
        // Do later
    });

    $(".check-property").change(function () {
        var parent = $(this).closest('.form-check');
        if (this.checked) {
            $('.property-ischoose', parent).val(true);
        } else {
            $('.property-ischoose', parent).val(false);
        }
    });

    $('.btn-content-add').on('click', function () {
        var parent = $(this).closest('.content');
        var lastE = $('.hid-newE:last').val();
        var newE = parseInt(lastE) + 1;
        var newEd = newE + 1;
        var imgsize = $('.hid-size-image').val();
        var data = {
            model: "Category",
            entity: newE,
            newed: newEd,
            imgsize: imgsize
        };

        $('.content:last', $('.content-area')).after($.templates("#tmplContent").render(data));

        $('.content-text').summernote({
            tabsize: 2,
            minHeight: 300,
            popover: {
                image: [],
                link: [],
                air: []
            }
        });

        $(".images").on('change', function () {
            changeImg(this, "Category");
        });
    });

    $('.content-text').summernote({
        //placeholder: 'Chi tiết sản phẩm',
        tabsize: 2,
        minHeight: 300,
        popover: {
            image: [],
            link: [],
            air: []
        }
    });

    deleteImg();

    mainImg();

    $(".images").on('change', function () {
        changeImg(this, "Category");
    });
});
