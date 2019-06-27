$(function () {
    registerAutoNumeric();

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

    function loadDataByDate() {
        var url = $('.link').val() + "?Ngay=" + $('.tu-ngay').val();
        window.location.replace(url);
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
});