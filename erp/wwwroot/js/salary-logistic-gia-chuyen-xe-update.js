$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#thang').on('change', function (e) {
        $('#filter-form').submit();
    });

    registerAutoNumeric();

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
                if (response.length === 0)
                    alert('Some error occured while uploading');
                else {
                    window.location.replace(response.url);
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

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        var $this = $(this);
        var frmValues = $this.serialize();

        // loading button
        $('.btn-submit').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-submit').html(loadingText);

        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            data: frmValues
        })
            .done(function (data) {
                if (data.result === true) {
                    toastr.success(data.message);
                    setTimeout(function () {
                        window.location.reload();
                    }, 1000);
                }
                else {
                    toastr.error(data.message);
                    $('.btn-submit').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    //$('select', $('.data-form')).prop('disabled', false);
                    //$('textarea', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });

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

    function registerAutoNumeric() {
        $('.numeric').each(function (i, obj) {
            enableNumeric($(obj));
        });
    }

    function enableNumeric(element) {
        var code = $(element).data('id');
        newInstall = new AutoNumeric('.' + code, { decimalPlaces: 0 });
    }
});


