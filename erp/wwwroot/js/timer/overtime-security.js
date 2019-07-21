$(function () {
    $('.page-click').on('click', function () {
        var parentForm = $(this).closest('form');
        $('#trang-fpage', parentForm).val($(this).attr("data-page"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('#TrangDll').on('change', function () {
        var parentForm = $(this).closest('form');
        $('#trang-fpage', parentForm).val($(this).val());
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.btn-sort').on('click', function () {
        var parentForm = $(this).closest('form');
        $('.sap-xep', parentForm).val($(this).attr("data-sortby"));
        $('.thu-tu', parentForm).val($(this).attr("data-sortorder"));
        document.getElementById($(this).closest('form').attr('id')).submit();
    });

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.from-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.tu-ngay').val(date.format('MM-DD-YYYY'));
        formSubmit();
    });

    $('.to-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.den-ngay').val(date.format('MM-DD-YYYY'));
        formSubmit();
    });

    $('#mainChkBox').click(function () {
        if ($(this).is(':checked')) {
            $('.chk-item').prop('checked', true);
            $('.chk-item').val(true);
        } else {
            $('.chk-item').prop('checked', false);
            $('.chk-item').val(false);
        }
    });

    registerTimePicker();

    function registerTimePicker() {
        var no = $('.iE-value:last').val();
        for (i = 0; i <= no; ++i) {
            $('#start-' + i).datetimepicker({
                use24hours: true,
                format: 'HH:mm',
                widgetPositioning: {
                    horizontal: 'auto',
                    vertical: 'bottom'
                }
            });
            $('#start-' + i).on('change.datetimepicker', function () {
                calOvertime($(this));
            });
            $('#end-' + i).datetimepicker({
                use24hours: true,
                format: 'HH:mm',
                widgetPositioning: {
                    horizontal: 'auto',
                    vertical: 'bottom'
                }
            });
            $('#end-' + i).on('change.datetimepicker', function () {
                calOvertime($(this));
            });
        }
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
        $('.hSecurity-' + i).text(hours);
        $('#hHourSecurity-' + i).val(hours);
    }
});