$(function () {
    $('.leave-home-submit').on("submit", function (event) {
        event.preventDefault();
        var $this = $(this);
        var formData = new FormData($this[0]);
        // loading button
        $('.btn-submit').prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-submit', $this).html(loadingText);

        var frmValues = $this.serialize();
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
                    toastr.success(data.message);
                    // remove this records
                    $this.closest('.leave-item').remove();
                    // cal number total
                    var totalApproveLeave = parseInt($('.leave-approve-total').html()) - 1;
                    $('.leave-approve-total').html(totalApproveLeave);
                }
                else {
                    toastr.error(data.message);
                }
                $('.btn-submit').prop('disabled', false);
                $('.btn-submit', $this).html($('.btn-submit', $this).data('original-text'));
            })
            .fail(function () {
                toastr.error(data.message);
                $('.btn-submit', $this).prop('disabled', false);
                $('.btn-submit', $this).html($('.btn-submit', $this).data('original-text'));
            });
        event.preventDefault();
    });
});

function registerTimePicker() {
    var dateNow = new Date();
    $("#from_date").datepicker({
        language: "vi",
        format: 'dd/mm/yyyy',
        startDate: dateNow
    });

    $("#to_date").datepicker({
        language: "vi",
        format: 'dd/mm/yyyy',
        startDate: dateNow
    });

    $('#from_date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')._d;
        $('#to_date').val($(this).val());
        $('#to_date').datepicker('destroy');
        $("#to_date").datepicker({
            language: "vi",
            format: 'dd/mm/yyyy',
            startDate: date
        });
        calculatorLeaveDuration();
    });

    $('#to_date').on('changeDate', function () {
        calculatorLeaveDuration();
    });

    var startTimeHH = parseInt($('#Leave_Start').val().split(':')[0]);
    var startTimeMM = parseInt($('#Leave_Start').val().split(':')[1]);
    var endTimeHH = parseInt($('#Leave_End').val().split(':')[0]);
    var endTimeMM = parseInt($('#Leave_End').val().split(':')[1]);
    $('#leave-start').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(startTimeHH).minutes(startTimeMM).seconds(0).milliseconds(0)
    });
    $('#leave-start').on('change.datetimepicker', function () {
        calculatorLeaveDuration();
    });
    $('#leave-end').datetimepicker({
        locale: 'vi',
        format: 'LT',
        defaultDate: moment(dateNow).hours(endTimeHH).minutes(endTimeMM).seconds(0).milliseconds(0)
    });
    $('#leave-end').on('change.datetimepicker', function () {
        calculatorLeaveDuration();
    });
}

function calculatorLeaveDuration() {
    // Ajax calculator on server (holidays, sunday,...)
    var from = moment($('#from_date').datepicker('getFormattedDate'), 'DD-MM-YYYY')._d;
    from.setHours($('#leave-start').val().split(':')[0]);
    from.setMinutes($('#leave-start').val().split(':')[1]);
    var to = moment($('#to_date').datepicker('getFormattedDate'), 'DD-MM-YYYY')._d;
    to.setHours($('#leave-end').val().split(':')[0]);
    to.setMinutes($('#leave-end').val().split(':')[1]);

    //MM-DD-YYYY

    var fromPost =
        from.getUTCFullYear() + "/" +
        ("0" + (from.getUTCMonth() + 1)).slice(-2) + "/" +
        ("0" + from.getUTCDate()).slice(-2) + " " +
        ("0" + from.getHours()).slice(-2) + ":" +
        ("0" + from.getUTCMinutes()).slice(-2);
    var toPost = to.getUTCFullYear() + "/" +
        ("0" + (to.getUTCMonth() + 1)).slice(-2) + "/" +
        ("0" + to.getUTCDate()).slice(-2) + " " +
        ("0" + to.getHours()).slice(-2) + ":" +
        ("0" + to.getUTCMinutes()).slice(-2);

    var fromEntity = ("0" + (from.getUTCMonth() + 1)).slice(-2) + "-" + ("0" + from.getUTCDate()).slice(-2) + "-" + from.getUTCFullYear();
    var toEntity = ("0" + (to.getUTCMonth() + 1)).slice(-2) + "-" + ("0" + to.getUTCDate()).slice(-2) + "-" + to.getUTCFullYear();
    $('input[name="Leave.From"]').val(fromEntity);
    $('input[name="Leave.To"]').val(toEntity);
    $('input[name="Leave.Start"]').val($('#leave-start').val());
    $('input[name="Leave.End"]').val($('#leave-end').val());

    $.ajax({
        type: "POST",
        url: $('#hidCalculatorLink').val(),
        data: {
            from: fromPost,
            to: toPost,
            scheduleWorkingTime: $('#Leave_WorkingScheduleTime').val(),
            type: $('#Leave_TypeId').val()
        },
        success: function (data) {
            console.log(data);
            if (data.result === true) {
                $('.leave-duration').text(data.date);
            }
            else {
                $('.leave-duration').text(data.message);
            }
        }
    });
    
}

