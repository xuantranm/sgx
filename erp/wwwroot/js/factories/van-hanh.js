$(function () {
    setValueDateFormat();

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.hidedatepicker', $(this).closest('.date-area')).val(
            date.format('MM-DD-YYYY')
        );
    });
});
