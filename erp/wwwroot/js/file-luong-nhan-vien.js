$(document).ready(function () {
    $('#btnUpload').on('click', function () {
        var fileExtension = ['xls', 'xlsx', 'XLS', 'XLSX'];
        var filename = $('#fUpload').val();
        if (filename.length === 0) {
            alert("Please select a file.");
            return false;
        }
        else {
            var extension = filename.replace(/^.*\./, '');
            console.log(extension);
            if ($.inArray(extension, fileExtension) === -1) {
                alert("Please select only excel files.");
                return false;
            }
        }
        var fdata = new FormData();
        var fileUpload = $("#fUpload").get(0);
        var files = fileUpload.files;
        fdata.append(files[0].name, files[0]);
        fdata.append("sheetCal", 0);
        fdata.append("headerCal", 7);
        //console.log(fdata);
        $.ajax({
            type: "POST",
            url: "/tai-lieu/luong-nhan-vien/import/",
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
                $('#dvData').html(e.responseText);
            },
            progress: downloadProgress,
            uploadProgress: uploadProgress
        });
    });

    var $progress = $('#progress')[0];

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