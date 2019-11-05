$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#Content_CategoryId').on('change', function () {
        $.ajax({
            type: 'get',
            url: '/category/getproperties',
            data: { id: $(this).val() },
            success: function (data) {
                if (data.result === true) {
                    $('.property').remove();
                    $.each(data.properties, function (key, item) {
                        if (item.key === "image-size") {
                            if (item.value !== null) {
                                $('.badge-image').html(item.value);
                            }
                        }
                        else {
                            $('.properties').prepend($.templates("#tmplProperty").render(item));
                        }
                    });
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
        
    });

    $('#newCategoryModal').on('show.bs.modal', function (event) {
        //var modal = $(this);
        //eventModal(modal, "chucvu");
    });

    $('.btn-save-category').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues,
            success: function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    $('select[name="Content.CategoryId"] option:first').after('<option value="' + data.entity.id + '">' + data.entity.name + '</option>');
                    $('select[name="Content.CategoryId"]').val(data.entity.id);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newCategoryModal').modal('hide');
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });

    $('textarea.js-auto-size').textareaAutoSize();

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
        changeImg(this, "Content");
    });

    $('.image-single-input').on('change', function () {
        changeImgSingle(this, "Content");
    });
});
