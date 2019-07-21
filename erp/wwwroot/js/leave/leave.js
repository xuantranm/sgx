$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlEmployeeId').on('change', function () {
        formSubmit();
    });

    $('input[name="Leave.TypeName"]').val($('select[name="Leave.TypeId"] option:selected').text());

    $('select[name="Leave.TypeId"]').on('change', function () {
        $('input[name="Leave.TypeName"]').val($('select[name="Leave.TypeId"] option:selected').text());
    });

    $('input[name="Leave.ApproverName"]').val($('select[name="Leave.ApproverId"] option:selected').text());

    $('select[name="Leave.ApproverId"]').on('change', function () {
        $('input[name="Leave.ApproverName"]').val($('select[name="Leave.ApproverId"] option:selected').text());
    });

    //$('.btn-chart-save').on('click', function () {
    //    $('#chart-config').collapse('hide');
    //    loadChart();
    //});

    //loadChart();

    registerTimePicker();

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        if ($('#Leave_EmployeeId').val() === "") {
            toastr.error("Vui lòng chọn nhân viên!");
            return false;
        }

        if (!confirm("Bạn chắc chắn thông tin đã được kiểm tra và muốn cập nhật!")) {
            return false;
        }

        var $this = $(this);
        var formData = new FormData($this[0]);
        // loading button
        $('.btn-submit', $this).prop('disabled', true);
        $('input', $this).prop('disabled', true);
        $('select', $this).prop('disabled', true);
        $('textarea', $this).prop('disabled', true);
        $('#btnSubmitLeave').html('<i class="fas fa-spinner"></i> đang xử lý...');
        
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
                    setTimeout(function () {
                        window.location.reload();
                    }, 1000);
                }
                else {
                    toastr.error(data.message);
                    $('.btn-submit', $this).prop('disabled', false);
                    $('input', $this).prop('disabled', false);
                    $('select', $this).prop('disabled', false);
                    $('textarea', $this).prop('disabled', false);
                    $('.btn-submit', $this).html($('.btn-submit', $this).data('original-text'));
                }
            })
            .fail(function () {
                toastr.error(data.message);
                $('.btn-submit', $this).prop('disabled', false);
                $('.btn-submit', $this).html($('.btn-submit', $this).data('original-text'));
            });
    });

    function registerTimePicker() {
        var dateNow = new Date();
        $("#from_date").datepicker({
            language: "vi",
            format: 'dd/mm/yyyy'
            //startDate: dateNow
        });

        $("#to_date").datepicker({
            language: "vi",
            format: 'dd/mm/yyyy'
            //startDate: dateNow
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
        console.log(moment(dateNow).hours(startTimeHH).minutes(startTimeMM).seconds(0).milliseconds(0));
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
        console.log(from);
        console.log(to);
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

        console.log("Fro: " + fromPost);
        console.log("To: " + toPost);
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


    function loadChart() {
        resetCanvas();
        var chartType = $('.chart-type').val() ? $('.chart-type').val() : "bar";
        var chartCategory = $('.chart-category').val();

        $.ajax({
            url: "/chart/datahr",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: {
                type: chartType,
                category: chartCategory
            },
            success: function (data) {
                console.log(data);
                $('.chart-name').text(data.title);
                $('.chart-info').text(data.info);
                datasets = [{
                    label: data.title,
                    data: data.data,
                    backgroundColor: data.backgroundColor,
                    borderColor: data.borderColor,
                    borderWidth: data.borderWidth
                }];
                var ctx = document.getElementById("chart");
                var config = {
                    type: data.type,
                    data: {
                        labels: data.labels,
                        datasets: datasets
                    },
                    options: {
                        responsive: true
                        //title: {
                        //    display: true,
                        //    text: data.title
                        //}
                    }
                };
                var myChart = new Chart(ctx, config);
                //window.myPie = new Chart(ctx, config);
            },
            error: function (rtnData) {
                alert('error' + rtnData);
            }
        });
    }

    function resetCanvas() {
        $('#chart').remove(); // this is my <canvas> element
        $('.grap-container').append('<canvas id="chart"><canvas>');
    }
});

