$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.left-menu').addClass('d-none');

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlNl').on('change', function () {
        changeByCongTyChiNhanh($(this).val());
    });

    $('.ddlKcn').on('change', function () {
        changeByKhoiChucNang($(this).val());
    });

    $('.ddlPb').on('change', function () {
        changeByPhongBan($(this).val());
    });

    $('.ddlBp').on('change', function () {
        changeByBoPhan($(this).val());
    });

    $('.ddlEmployeeId').on('change', function () {
        formSubmit();
    });

    $('#Thang').on('change', function (e) {
        formSubmit();
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

    tangca = new AutoNumeric('.form-control-tang-ca', { decimalPlaces: 2 });

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

    function changeByCongTyChiNhanh(congtychinhanh) {
        $.ajax({
            type: "GET",
            url: "/api/GetByCongTyChiNhanh",
            contentType: "application/json; charset=utf-8",
            data: { congtychinhanh: congtychinhanh },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $kcn = $(".ddlKcn");
                    $kcn.empty();
                    if (data.khoichucnangs.length > 1) {
                        $kcn.append($("<option></option>")
                            .attr("value", "").text("Chọn"));
                    }
                    $.each(data.khoichucnangs, function (key, khoichucnang) {
                        $kcn.append($("<option></option>")
                            .attr("value", khoichucnang.id).text(khoichucnang.name));
                    });

                    if (data.khoichucnangs.length === 1) {
                        changeByKhoiChucNang($('.ddlKcn').val());
                    }

                    var $pb = $(".ddlPb");
                    $pb.empty();

                    var $bp = $(".ddlBp");
                    $bp.empty();
                }
            }
        });
    }

    function changeByKhoiChucNang(khoichucnang) {
        $.ajax({
            type: "GET",
            url: "/api/GetByKhoiChucNang",
            contentType: "application/json; charset=utf-8",
            data: { khoichucnang: khoichucnang },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    $('.ddlNl').val(data.congTyChiNhanhId);

                    var $pb = $(".ddlPb");
                    $pb.empty();
                    if (data.phongbans.length > 1) {
                        $pb.append($("<option></option>")
                            .attr("value", "").text("Chọn"));
                    }
                    $.each(data.phongbans, function (key, phongban) {
                        $pb.append($("<option></option>")
                            .attr("value", phongban.id).text(phongban.name));
                    });

                    if (data.phongbans.length === 1) {
                        changeByPhongBan($('.ddlPb').val());
                    }
                }
            }
        });
    }

    function changeByPhongBan(phongban) {
        $.ajax({
            type: "GET",
            url: "/api/GetByPhongBan",
            contentType: "application/json; charset=utf-8",
            data: { phongban: phongban },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $bp = $(".ddlBp");
                    $bp.empty();
                    if (data.bophans.length > 1) {
                        $bp.append($("<option></option>")
                            .attr("value", "").text("Chọn"));
                    }
                    $.each(data.bophans, function (key, bophan) {
                        $bp.append($("<option></option>")
                            .attr("value", bophan.id).text(bophan.name));
                    });
                }
            }
        });
    }

    function changeByBoPhan(bophan) {
        formSubmit();
    }

});


