$(function () {
    $('.page-click').on('click', function () {
        var parentForm = $(this).closest('form');
        $('#trang-fpage', parentForm).val($(this).attr("data-page"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('#TrangDll').on('change', function () {
        var parentForm = $(this).closest('form');
        $('#trang-fpage', parentForm).val($(this).val());
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.btn-sort').on('click', function () {
        var parentForm = $(this).closest('form');
        $('.sap-xep', parentForm).val($(this).attr("data-sortby"));
        $('.thu-tu', parentForm).val($(this).attr("data-sortorder"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.btnUpload').on('click', function () {
        // Use funftion in site.js UPLOAD FILE
        var parent = $(this).closest('form');
        var fileExtension = ['xls', 'xlsx'];
        var filename = $('.fUpload', parent).val();
        if (filename.length === 0) {
            toastr.error("Please select a file.");
            return false;
        }
        else {
            var extension = filename.replace(/^.*\./, '');
            if ($.inArray(extension, fileExtension) === -1) {
                toastr.warning("Please select only excel files.");
                return false;
            }
        }
        showProgress(parent);
        var fdata = new FormData();
        var fileUpload = $(".fUpload", parent).get(0);
        var files = fileUpload.files;
        fdata.append(files[0].name, files[0]);
        var url = parent.attr('action');
        var type = parent.attr('method');
        $.ajax({
            type: type,
            url: url,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: fdata,
            contentType: false,
            processData: false,
            success: function (response) {
                resetFormUpload(parent);
                console.log(response);
                if (response.length === 0)
                    toastr.warning('Phát sinh lỗi tải tài liệu. Tải lại trang hoặc liên hệ IT hỗ trợ');
                else {
                    if (response.errors.length > 0) {
                        var htmlerrors = "";
                        $.each(response.errors, function (k, v) {
                            htmlerrors += "<span>"+ v +"</span><br />";
                        });
                        $('.error').removeClass('d-none');
                        $('.error').html(htmlerrors);
                    }
                    else {
                        toastr.success('Cập nhật thành công. Chúc bạn ngày làm việc vui vẻ.');
                        location.reload();
                    }
                }
            },
            error: function (e) {
                $('.dvData', parent).html(e.responseText);
            },
            progress: downloadProgress,
            uploadProgress: uploadProgress
        });
    });

    $('.from-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.tu-ngay').val(date.format('MM-DD-YYYY'));
        formSubmit();
    });

    $('.to-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.den-ngay').val(date.format('MM-DD-YYYY'));
        formSubmit();
    });
});