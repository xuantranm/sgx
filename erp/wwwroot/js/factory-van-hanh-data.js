$(function () {
    setValueFactory();

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    registerTimePicker();

    registerAutoNumeric();

    $('.btn-save-product').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues
        })
            .done(function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    // Update ddl
                    $('select[name="Entity.Product"] option:first').after('<option value="' + data.entity.name + "-" + data.entity.unit + '">' + data.entity.name + '</option>');
                    $('select[name="Entity.Product"]').val(data.entity.name + "-" + data.entity.unit);
                    $('input[name="Entity.Unit"]').val(data.entity.unit);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newProduct').modal('hide');
                }
                else {
                    toastr.error(data.message);
                }
            })
            .fail(function () {
                toastr.error(data.message)
            });
        event.preventDefault();
    });

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);

        //console.log(formData);
        var $this = $(this);
        //var frmValues = $this.serialize();
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,  // Important!
            contentType: false,
            data: formData
        })
            .done(function (data) {
                if (data.result === true) {
                    $('#resultModal').modal();
                }
                else {
                    toastr.error(data.message);
                }
            })
            .fail(function () {
                toastr.error(data.message);
            });
        event.preventDefault();
    });
});

function setValueFactory() {
    $('.datepicker').each(function (i, obj) {
        var date = moment($(obj).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.hidedatepicker', $(obj).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });
}

function registerTimePicker() {
    var dateNow = new Date();
    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(this).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });

    $('#Entity_Start').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(7).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_End').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(16).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_ThoiGianBTTQ').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(0).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_ThoiGianXeHu').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(0).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_ThoiGianNghi').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(1).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_ThoiGianCVKhac').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(0).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_ThoiGianDayMoBat').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(0).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_ThoiGianBocHang').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(0).minutes(0).seconds(0).milliseconds(0)
    });

    $('#Entity_Start').on('change.datetimepicker', function () {
        calculatorWorking();
    });

    $('#Entity_End').on('change.datetimepicker', function () {
        calculatorWorking();
    });

    $('#Entity_ThoiGianBTTQ').on('change.datetimepicker', function () {
        calculatorWorking();
    });

    $('#Entity_ThoiGianXeHu').on('change.datetimepicker', function () {
        calculatorWorking();
    });

    $('#Entity_ThoiGianNghi').on('change.datetimepicker', function () {
        calculatorWorking();
    });

    $('#Entity_ThoiGianCVKhac').on('change.datetimepicker', function () {
        calculatorWorking();
        calculatorTongThoiGianCVKhac();
    });

    $('#Entity_ThoiGianDayMoBat').on('change.datetimepicker', function () {
        calculatorWorking();
        calculatorTongThoiGianDayMoBat();
    });

    $('#Entity_ThoiGianBocHang').on('change.datetimepicker', function () {
        calculatorWorking();
        calculatorTongThoiGianBocHang();
    });
}

function registerAutoNumeric() {
    slnhancongAutoNumeric = new AutoNumeric('.slnhancong', { decimalPlaces: 0 });
    soluongthuchienAutoNumeric = new AutoNumeric('.soluongthuchien', { decimalPlaces: 2, allowDecimalPadding: false });
    soluongdonggoiAutoNumeric = new AutoNumeric('.soluongdonggoi', { decimalPlaces: 2, allowDecimalPadding: false });
    soluongbochangAutoNumeric = new AutoNumeric('.soluongbochang', { decimalPlaces: 2, allowDecimalPadding: false });
    dauAutoNumeric = new AutoNumeric('.dau', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot10AutoNumeric = new AutoNumeric('.nhot10', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot50AutoNumeric = new AutoNumeric('.nhot50', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot90AutoNumeric = new AutoNumeric('.nhot90', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot140AutoNumeric = new AutoNumeric('.nhot140', { decimalPlaces: 2, allowDecimalPadding: false });

    $('.slnhancong').on('keyup', function () {
        //var trueVal = Number(slnhancongAutoNumeric.getNumericString());
        //$('#Entity_SLNhanCong').val(trueVal);
        calculatorTongThoiGianBocHang();
        calculatorTongThoiGianDongGoi();
        calculatorTongThoiGianCVKhac();
        calculatorTongThoiGianDayMoBat();
    });
}

function calculatorWorking() {
    // Convert all dates to milliseconds
    var end_ms = calculatorHmsToSecond($('#Entity_End').val() +":00");
    var start_ms = calculatorHmsToSecond($('#Entity_Start').val() + ":00");
    var bttq_ms = calculatorHmsToSecond($('#Entity_ThoiGianBTTQ').val() + ":00");
    var xehu_ms = calculatorHmsToSecond($('#Entity_ThoiGianXeHu').val() + ":00");
    var nghi_ms = calculatorHmsToSecond($('#Entity_ThoiGianNghi').val() + ":00");
    var cvkhac_ms = calculatorHmsToSecond($('#Entity_ThoiGianCVKhac').val() + ":00");
    var daymobat_ms = calculatorHmsToSecond($('#Entity_ThoiGianDayMoBat').val() + ":00");
    var bochang_ms = calculatorHmsToSecond($('#Entity_ThoiGianBocHang').val() + ":00");
    var seconds = (end_ms - start_ms) - bttq_ms - xehu_ms - nghi_ms - cvkhac_ms - daymobat_ms - bochang_ms;
    var display = secondToHHMM(seconds);
    $('#ThoiGianLamViec_second').val(seconds);
    $('#Entity_ThoiGianLamViec').val(display);
    $('.ThoiGianLamViec').text(display);
    calculatorTongThoiGianDongGoi();
}

function calculatorTongThoiGianBocHang() {
    var slNhanCong = Number(slnhancongAutoNumeric.getNumericString());
    var second = calculatorHmsToSecond($('#Entity_ThoiGianBocHang').val() + ":00");
    var resultSecond = second * slNhanCong;
    var display = secondToHHMM(resultSecond);
    $('#Entity_TongThoiGianBocHang').val(resultSecond);
    $('.TongThoiGianBocHang').text(display);
}

function calculatorTongThoiGianDongGoi() {
    var slNhanCong = Number(slnhancongAutoNumeric.getNumericString());
    var second = $('#ThoiGianLamViec_second').val();
    var resultSecond = second * slNhanCong;
    var display = secondToHHMM(resultSecond);
    $('#Entity_TongThoiGianDongGoi').val(resultSecond);
    $('.TongThoiGianDongGoi').text(display);
}

function calculatorTongThoiGianCVKhac() {
    var slNhanCong = Number(slnhancongAutoNumeric.getNumericString());
    var second = calculatorHmsToSecond($('#Entity_ThoiGianCVKhac').val() + ":00");
    var resultSecond = second * slNhanCong;
    var display = secondToHHMM(resultSecond);
    $('#Entity_TongThoiGianCVKhac').val(resultSecond);
    $('.TongThoiGianCVKhac').text(display);
}

function calculatorTongThoiGianDayMoBat() {
    var slNhanCong = Number(slnhancongAutoNumeric.getNumericString());
    var second = calculatorHmsToSecond($('#Entity_ThoiGianDayMoBat').val() + ":00");
    var resultSecond = second * slNhanCong;
    var display = secondToHHMM(resultSecond);
    $('#Entity_TongThoiGianDayMoBat').val(resultSecond);
    $('.TongThoiGianDayMoBat').text(display);
}
