$(function () {
    $('input[name="Leave.TypeName"]').val($('select[name="Leave.TypeId"] option:selected').text());

    $('select[name="Leave.TypeId"]').on('change', function () {
        $('input[name="Leave.TypeName"]').val($('select[name="Leave.TypeId"] option:selected').text());
    });

    $('.datetimes').daterangepicker({
        timePicker: true,
        //"autoApply": true, notworking use paralel timepicker
        startDate: moment().startOf('hour').set({ hour: 07, minute: 0, second: 0 }),
        endDate: moment().startOf('hour').set({ hour: 17, minute: 0, second: 0 }),
        //"minDate": moment().set({ hour: 0, minute: 0, second: 0 }),
        locale: {
            format: 'DD/MM/YYYY hh:mm A'
        }
    }, function (start, end, label) {
        console.log('New date range selected: ' + start.format('YYYY-MM-DD') + ' to ' + end.format('YYYY-MM-DD') + ' (predefined range: ' + label + ')');
        $('input[name="Leave.From"]').val(start.format('MM-DD-YYYY HH:mm'));
        $('input[name="Leave.To"]').val(end.format('MM-DD-YYYY HH:mm'));
        //6/13/2018 3:26:51 PM
        console.log(start.toString());
    });
});
