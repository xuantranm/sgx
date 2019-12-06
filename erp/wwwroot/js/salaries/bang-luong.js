$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.btn-sort').on('click', function () {
        var parentForm = $(this).closest('form');
        $('.sap-xep', parentForm).val($(this).attr("data-sortby"));
        $('.thu-tu', parentForm).val($(this).attr("data-sortorder"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.ddlkhoichucnang').on('change', function () {
        loadCategory($(this).val(), $('.ephongban-val-hide').val(), $('.ddlphongban')); // parent , data
    });

    $('.btn-edit').on('click', function () {
        var parentId = $(this).data('parentid');
        $('.edit-' + parentId).removeClass('d-none');
    });

    $('.btn-xac-nhan').on('click', function () {
        var parentId = $(this).data('parentid');
        event.preventDefault();
        //grab all form data  
        var formData = $('.form-' + parentId).find('select, textarea, input').serialize();
        console.log(formData);
        //// loading button
        $(this).prop('disabled', true);
        $('.form-' + parentId).find('select, textarea, input').prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $(this).html(loadingText);
        $.ajax({
            type: 'post',
            url: 'bang-luong/van-phong/update',
            data: formData,
            success: function (data) {
                console.log(data);
                formSubmit();
            }
        });
    });

    function loadCategory(parentId, type, objectLoad) {
        console.log("parent:" + parentId + "; type:" + type);
        $.ajax({
            type: "GET",
            url: "/api/loadcategory",
            contentType: "application/json; charset=utf-8",
            data: { parentid: parentId, type: type },
            dataType: "json",
            success: function (data) {
                console.log(data);
                if (data.result === true) {
                    objectLoad.empty();
                    if (data.categories.length > 1) {
                        objectLoad.append($("<option></option>").attr("value", "").text("Chọn"));
                    }
                    $.each(data.categories, function (key, item) {
                        objectLoad.append($("<option></option>").attr("value", item.id).text(item.name));
                    });
                }
            }
        });
    }
});