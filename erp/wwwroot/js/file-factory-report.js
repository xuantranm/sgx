$(document).ready(function () {
    $('.btnUpload').on('click', function () {
        var parent = $(this).closest('form');
        var fileExtension = ['xls', 'xlsx'];
        var filename = $('.fUpload', parent).val();
        if (filename.length == 0) {
            alert("Please select a file.");
            return false;
        }
        else {
            var extension = filename.replace(/^.*\./, '');
            if ($.inArray(extension, fileExtension) == -1) {
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
        console.log(url);
        console.log(type);
        //fdata.append("sheetCal", 1);
        //fdata.append("headerCal", 4);
        //console.log(fdata);
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
                if (response.length == 0)
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
    })

    var $progress = $('.progress', parent)[0];

    function uploadProgress(e) {
        if (e.lengthComputable) {
            //console.log("total:" + e.total)
            var percentComplete = (e.loaded * 100) / e.total;
            //console.log(percentComplete);
            $('.progress-bar', $progress).css('width', percentComplete +"%")
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
            $('.progress-bar', $progress).css('width', percentage + "%")

            if (percentage >= 100) {
                // process completed
            }
        }
    }
});