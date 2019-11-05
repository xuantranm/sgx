$(function () {
    setValueDatePicker();

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    registerTimePicker();

    registerAutoNumeric();

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



function registerTimePicker() {
    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.hidedatepicker', $(this).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });

    // Register
    $('#start').datetimepicker({
        locale: 'vi',
        format: 'LT'
    });
    $('#end').datetimepicker({
        locale: 'vi',
        format: 'LT'
    });
    $('#tgBTTQ').datetimepicker({
        locale: 'vi',
        format: 'LT'
    });
    $('#tgXeHu').datetimepicker({
        locale: 'vi',
        format: 'LT'
    });
    $('#tgNghi').datetimepicker({
        locale: 'vi',
        format: 'LT'
    });
    $('#tgCVKhac').datetimepicker({
        locale: 'vi',
        format: 'LT'
    });

    // Set value
    var mode = $('#mode').val();
    if (mode === "False") {
        $('#start').val("00:00");
        $('#end').val("00:00");
        $('#tgBTTQ').val("00:00");
        $('#tgXeHu').val("00:00");
        $('#tgNghi').val("00:00");
        $('#tgCVKhac').val("00:00");
    }
    else {
        $('#start').val($('#hidStart').val());
        $('#end').val($('#hidEnd').val());
        $('#tgBTTQ').val($('#hidTgBTTQ').val());
        $('#tgXeHu').val($('#hidTgXeHu').val());
        $('#tgNghi').val($('#hidTgNghi').val());
        $('#tgCVKhac').val($('#hidTgCVKhac').val());
    }
    // Event
    $('#start').on('change.datetimepicker', function () {
        calculatorWorkingVanHanh();
    });
    $('#end').on('change.datetimepicker', function () {
        calculatorWorkingVanHanh();
    });
    $('#tgBTTQ').on('change.datetimepicker', function () {
        calculatorWorkingVanHanh();
    });
    $('#tgNghi').on('change.datetimepicker', function () {
        calculatorWorkingVanHanh();
    });
    $('#tgXeHu').on('change.datetimepicker', function () {
        calculatorWorkingVanHanh();
    });
    $('#tgCVKhac').on('change.datetimepicker', function () {
        calculatorWorkingVanHanh();
    });
}

function registerAutoNumeric() {
    soluongthuchienAutoNumeric = new AutoNumeric('.soluongthuchien', { decimalPlaces: 2, allowDecimalPadding: false });
    dauAutoNumeric = new AutoNumeric('.dau', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot10AutoNumeric = new AutoNumeric('.nhot10', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot50AutoNumeric = new AutoNumeric('.nhot50', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot90AutoNumeric = new AutoNumeric('.nhot90', { decimalPlaces: 2, allowDecimalPadding: false });
    nhot140AutoNumeric = new AutoNumeric('.nhot140', { decimalPlaces: 2, allowDecimalPadding: false });
}

function calculatorWorkingVanHanh() {
    // Convert all dates to milliseconds
    var end_ms = calculatorHmsToSecond($('#end').val() +":00");
    var start_ms = calculatorHmsToSecond($('#start').val() + ":00");
    var bttq_ms = calculatorHmsToSecond($('#tgBTTQ').val() + ":00");
    var xehu_ms = calculatorHmsToSecond($('#tgXeHu').val() + ":00");
    var nghi_ms = calculatorHmsToSecond($('#tgNghi').val() + ":00");
    var cvkhac_ms = calculatorHmsToSecond($('#tgCVKhac').val() + ":00");
    //var seconds = end_ms - start_ms - bttq_ms - xehu_ms - nghi_ms - cvkhac_ms - daymobat_ms - bochang_ms;
    var seconds = end_ms - start_ms - bttq_ms - xehu_ms - nghi_ms - cvkhac_ms;
    var display = secondToHHMM(seconds);
    //$('#ThoiGianLamViec_second').val(seconds);
    //$('#Entity_ThoiGianLamViec').val(display);
    $('#Entity_ThoiGianLamViec').val(display);
    //calculatorTongThoiGianDongGoi();
}
