$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlkhoichucnang').on('change', function () {
        loadCategory($(this).val(), $('.ephongban-val-hide').val(), $('.ddlphongban')); // parent , data
    });

    $('.from-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.tu-ngay').val(
            date.format('MM-DD-YYYY')
        );
    });

    $('.to-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.den-ngay').val(
            date.format('MM-DD-YYYY')
        );
    });

    //tangca = new AutoNumeric('.form-control-tang-ca', { decimalPlaces: 2 });

    $('.btn-edit-overtime').on('click', function () {
        var parentId = $(this).data('parentid');
        $('.edit-overtime-' + parentId).removeClass('d-none');
    });

    $('.btn-xac-nhan-overtime').on('click', function () {
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
            url: 'tang-ca/hr-xac-nhan',
            data: formData,
            success: function (data) {
                formSubmit();
            }
        });
    });

    $('.xac-nhan-tang-ca-form').on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);
        // loading button
        $('.btn-xac-nhan-tang-ca', $(this)).prop('disabled', true);
        $('.btn-khong-xac-nhan-tang-ca', $(this).closest('.card')).prop('disabled', true);
        $('input', $(this)).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-xac-nhan-tang-ca', $(this)).html(loadingText);
        var $this = $(this);
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,
            contentType: false,
            data: formData,
            success: function (data) {
                // ajax later
                console.log(data);
                formSubmit();
            }
        });
    });

    $('.khong-xac-nhan-tang-ca-form').on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);
        // loading button
        $('.btn-xac-nhan-tang-ca', $(this).closest('.card')).prop('disabled', true);
        $('.btn-khong-xac-nhan-tang-ca', $(this)).prop('disabled', true);
        $('input', $(this)).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-khong-xac-nhan-tang-ca', $(this)).html(loadingText);
        var $this = $(this);
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,
            contentType: false,
            data: formData,
            success: function (data) {
                // ajax later
                console.log(data);
                formSubmit();
            }
        });
    });

    $('.btnUpload').on('click', function () {
        var parent = $(this).closest('form');
        var fileExtension = ['xls', 'xlsx'];
        var filename = $('.fUpload', parent).val();
        if (filename.length === 0) {
            alert("Please select a file.");
            return false;
        }
        else {
            var extension = filename.replace(/^.*\./, '');
            if ($.inArray(extension, fileExtension) === -1) {
                alert("Please select only excel files.");
                return false;
            }
        }
        var fdata = new FormData();
        var fileUpload = $(".fUpload", parent).get(0);
        var files = fileUpload.files;
        fdata.append(files[0].name, files[0]);
        var url = parent.attr('action');
        var type = parent.attr('method');
        // Stop duplicate button click
        $('.btnUpload').prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btnUpload').html(loadingText);
        $('.fUpload').prop('disabled', true);
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
                if (response.length === 0) {
                    toastr.error("Lỗi trong quá trình cập nhật dữ liệu.");
                    $('.btnUpload').prop('disabled', false);
                    var loadingText = 'Cập nhật';
                    $('.btnUpload').html(loadingText);
                    $('.fUpload').prop('disabled', false);
                }
                else {
                    toastr.success("Cập nhật dữ liệu thành công");
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                }
            },
            error: function (e) {
                $('.dvData', parent).html(e.responseText);
            },
            progress: downloadProgress,
            uploadProgress: uploadProgress
        });
    });

    var $progress = $('.progress', parent)[0];

    function uploadProgress(e) {
        if (e.lengthComputable) {
            //console.log("total:" + e.total)
            var percentComplete = (e.loaded * 100) / e.total;
            //console.log(percentComplete);
            $('.progress-bar', $progress).css('width', percentComplete + "%");
            //$progress.value = percentComplete;

            if (percentComplete >= 100) {
                // process completed
            }
        }
    }

    function downloadProgress(e) {
        if (e.lengthComputable) {
            var percentage = (e.loaded * 100) / e.total;
            //console.log(percentage);
            //$progress.value = percentage;
            $('.progress-bar', $progress).css('width', percentage + "%");

            if (percentage >= 100) {
                // process completed
            }
        }
    }

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


