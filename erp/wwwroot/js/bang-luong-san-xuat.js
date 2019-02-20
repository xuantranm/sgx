$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    //$('.left-menu').addClass('d-none');
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#Thang').on('change', function (e) {
        formSubmit();
    });

    $('#Id').on('change', function (e) {
        formSubmit();
    });

    $('#ddlMonthImport').on('change', function (e) {
        var hreftamung = $('.btn-link-tam-ung').attr("href");
        var hreftamung1 = hreftamung.split('?')[0];
        $('.btn-link-tam-ung').attr("href", hreftamung1 + "?thang=" + $('#ddlMonthImport').val());

        var hreftangca = $('.btn-link-tang-ca').attr("href");
        var hreftangca1 = hreftangca.split('?')[0];
        $('.btn-link-tang-ca').attr("href", hreftangca1 + "?thang=" + $('#ddlMonthImport').val());

        var hrefthuongbhxh = $('.btn-link-thuong-bhxh').attr("href");
        var hrefthuongbhxh1 = hrefthuongbhxh.split('?')[0];
        $('.btn-link-thuong-bhxh').attr("href", hrefthuongbhxh1 + "?thang=" + $('#ddlMonthImport').val());
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
                if (response.length === 0)
                    alert('Some error occured while uploading');
                else {
                    location.reload();
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

    function showProgress(parent) {
        $('.btnUpload', parent).prop('disabled', true);
        $('.btnUpload', parent).addClass('d-none');
        $('input', parent).prop('disabled', true);
        $('.btn-upload-process', parent).removeClass('d-none');
    }

    function resetFormUpload(parent) {
        $('.btnUpload', parent).removeClass('d-none');
        $('.btnUpload', parent).prop('disabled', false);
        $('input', parent).prop('disabled', false);
        $('.btn-upload-process', parent).addClass('d-none');
    }
});