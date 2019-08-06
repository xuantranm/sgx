$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlPbBp').on('change', function () {
        formSubmit();
    });

    $('#khoiChucNangModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget); // Button that triggered the modal
        var id = button.data('id'); // Extract info from data-* attributes
        var modal = $(this);
        $.ajax({
            type: "GET",
            url: "/api/GetByKhoiChucNang",
            contentType: "application/json; charset=utf-8",
            data: { id: id },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    if (data.khoichucnang !== '') {
                        modal.find('.id-modal').val(data.khoichucnang.id);
                        modal.find('.congtychinhanhid-modal').val(data.khoichucnang.congTyChiNhanhId);
                        modal.find('.name-modal').val(data.khoichucnang.name);
                        modal.find('.description-modal').val(data.khoichucnang.description);
                        modal.find('.order-modal').val(data.khoichucnang.order);
                        modal.find('.enable-modal').val(data.khoichucnang.enable);
                    }

                    var $khoichucnang = $('.existData', modal);
                    $khoichucnang.empty();
                    $.each(data.khoichucnangs, function (key, khoichucnang) {
                        $khoichucnang.append("<small class='badge badge-primary ml-1'>" + khoichucnang.order +". " + khoichucnang.name + "</small>");
                    });
                }
            }
        });
    });

    $('#phongBanBoPhanModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget); // Button that triggered the modal
        var id = button.data('id'); // Extract info from data-* attributes
        var modal = $(this);
        $.ajax({
            type: "GET",
            url: "/api/GetByPhongBan",
            contentType: "application/json; charset=utf-8",
            data: { id: id },
            dataType: "json",
            success: function (data) {
                console.log(data);
                if (data.result === true) {
                    if (data.phongban !== '') {
                        modal.find('.id-modal').val(data.phongban.id);
                        modal.find('.khoichucnangid-modal').val(data.phongban.khoiChucNangId);
                        modal.find('.name-modal').val(data.phongban.name);
                        modal.find('.description-modal').val(data.phongban.description);
                        modal.find('.order-modal').val(data.phongban.order);
                        modal.find('.enable-modal').val(data.phongban.enable);
                    }

                    var $phongban = $('.existData', modal);
                    $phongban.empty();
                    $.each(data.phongbans, function (key, phongban) {
                        $phongban.append("<small class='badge badge-primary ml-1'>" + phongban.order + ". " + phongban.name + "</small>");
                    });
                }
            }
        });

        eventModalPhongBan(modal);
    });

    function eventModalPhongBan(modal) {
        $('.khoichucnangid-modal', modal).on('change', function () {
            $.ajax({
                type: "GET",
                url: "/api/GetByKhoiChucNang",
                contentType: "application/json; charset=utf-8",
                data: { id: $(this).val() },
                dataType: "json",
                success: function (data) {
                    console.log(data);
                    if (data.result === true) {
                        var $phongban = $('.existData', modal);
                        $phongban.empty();
                        $.each(data.phongbans, function (key, phongban) {
                            $phongban.append("<small class='badge badge-primary ml-1'>" + phongban.order + ". " + phongban.name + "</small>");
                        });
                    }
                }
            });
        });
    }

    $('.btn-save-khoichucnang').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues,
            success: function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    $('select[name="Employee.KhoiChucNang"] option:first').after('<option value="' + data.entity.id + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.KhoiChucNang"]').val(data.entity.id);
                    $('select[name="Employee.KhoiChucNangName"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#khoiChucNangModal').modal('hide');
                    location.reload();
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });

    $('.btn-save-phongban').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues,
            success: function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    // Update ddl
                    $('select[name="Employee.PhongBan"] option:first').after('<option value="' + data.entity.id + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.PhongBan"]').val(data.entity.id);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#phongBanBoPhanModal').modal('hide');
                    location.reload();
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });
});

