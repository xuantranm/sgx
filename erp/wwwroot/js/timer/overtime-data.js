$(function () {
    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        if (!confirm("Bạn chắc chắn thông tin đã được kiểm tra và muốn cập nhật! LƯU Ý KHÔNG CHỈNH SỬA SAU KHI BẤM ĐỒNG Ý!")) {
            return false;
        }

        if ($('.hour-total').val() <= 0) {
            toastr.error("Dữ liệu không đúng: Số giờ 0");
            return false;
        }

        var $this = $(this);
        var formData = new FormData($this[0]);
        $('.btn-submit', $this).prop('disabled', true);
        $('input', $this).prop('disabled', true);
        $('select', $this).prop('disabled', true);
        $('textarea', $this).prop('disabled', true);
        $('.btn-submit', $this).html('<i class="fas fa-spinner"></i> đang xử lý...');

        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,  // Important!
            contentType: false,
            data: formData,
            success: function (data) {
                if (data.result === true) {
                    toastr.success(data.message);
                    setTimeout(function () {
                        window.location = data.href;
                    }, 1000);
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });

    $('.from-date').on('changeDate', function () {
        if ($('.isChange').val() === 1) {
            if (confirm("Anh chị muốn thay đổi ngày? Dữ liệu bên dưới chưa lưu sẽ mất, do tải dữ liệu mới theo ngày chọn!")) {
                var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
                $('.tu-ngay').val(date.format('MM-DD-YYYY'));
                loadDataByDate();
            }
            else {
                return false;
            }
        }
        else {
            var date2 = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
            $('.tu-ngay').val(date2.format('MM-DD-YYYY'));
            loadDataByDate();
        }
    });

    for (i = 0; i <= parseInt($('.no-overtime:last').val()); ++i) {
        registerTimePicker(i);
    }

    $('.btn-add-hour').on('click', function () {
        var lastNo = $('.no-overtime').last().val();
        $('.btn-time-remove-' + lastNo).addClass('d-none');
        var newNo = parseInt(lastNo) + 1;
        var data = [];
        data.push({ no: newNo });
        var newHtml = $.templates("#tmplTimes").render(data);
        $(newHtml).insertBefore(this);
        registerTimePicker(newNo);
        removeElementTime(newNo);
    });

    $('.check-agreement').change(function () {
        if (this.checked) {
            $('.btn-submit').prop('disabled', false);
        }
        else {
            $('.btn-submit').prop('disabled', true);
        }
    });

    loadValue();

    function loadDataByDate() {
        var url = $('.link').val() + "?Ngay=" + $('.tu-ngay').val();
        window.location.replace(url);
    }

    function removeElementTime(no) {
        $('.btn-time-remove-' + no).on('click', function () {
            var currentNo = parseInt($('.no-overtime', this.closest('.form-group')).val());
            if (currentNo > 1) {
                var previous = currentNo - 1;
                $('.btn-time-remove-' + previous).removeClass('d-none');
            }
            $(this.closest('.form-group')).remove();
        });
    }

    function registerTimePicker(no) {
        $('#start-' + no).val($('#hiddenStart-'+ no).val());
        $('#end-' + no).val($('#hiddenEnd-' + no).val());
        $('#start-' + no).datetimepicker({
            use24hours: true,
            format: 'HH:mm',
            widgetPositioning: {
                horizontal: 'auto',
                vertical: 'bottom'
            }
        });
        $('#start-' + no).on('change.datetimepicker', function () {
            calOvertime($(this));
        });
        $('#end-' + no).datetimepicker({
            use24hours: true,
            format: 'HH:mm',
            widgetPositioning: {
                horizontal: 'auto',
                vertical: 'bottom'
            }
        });
        $('#end-' + no).on('change.datetimepicker', function () {
            calOvertime($(this));
        });
    }

    function calOvertime(element) {
        var i = $(element).data('id');
        var time1 = $('#start-' + i).val().split(':');
        var time2 = $('#end-' + i).val().split(':');

        var hours1 = parseInt(time1[0], 10),
            hours2 = parseInt(time2[0], 10),
            mins1 = parseInt(time1[1], 10),
            mins2 = parseInt(time2[1], 10);
        var hours = hours2 - hours1, mins = 0;

        // get hours
        if (hours < 0) hours = 24 + hours;

        // get minutes
        if (mins2 >= mins1) {
            mins = mins2 - mins1;
        }
        else {
            mins = mins2 + 60 - mins1;
            hours--;
        }

        // convert to fraction of 60
        mins = mins / 60;

        hours += mins;
        hours = hours.toFixed(2);
        $('.hour-' + i).text(hours);
        $('.hour-item-' + i).val(hours);
        var hourtotal = 0.0;
        $('.hour-item').each(function (i, obj) {
            hourtotal += parseFloat($(obj).val());
        });
        $('.hour-total').val(hourtotal);
    }

    

    function registerAutoNumeric() {
        $('.numeric').each(function (i, obj) {
            enableNumeric($(obj));
        });
    }

    function enableNumeric(element) {
        var code = $(element).data('id');
        newInstall = new AutoNumeric('.' + code, { decimalPlaces: 1 });
        $('.' + code).on('keyup', function () {
            $('.isChange').val(1);
        });
    }

    function loadValue() {
        var no = parseInt($('.iE-value:last').val());
        for (i = 0; i <= no; ++i) {
            $('#start-' + i).val($('#hiddenStart-' + i).val());
            $('#end-' + i).val($('#hiddenEnd-' + i).val());
        }
    }
});