$(function () {
    setValueDateFormat();
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(this).closest('.date-area')).val(
            date.format('MM-DD-YYYY')
        );
    });

    $('#chartTonSX').on('shown.bs.modal', function (event) {
        var link = $(event.relatedTarget);
        // get data source
        var source = link.attr('data-source').split(',');
        // get title
        var title = link.html();
        // get labels
        var table = link.parents('table');
        var labels = [];
        $('#' + table.attr('id') + '>thead>tr>th').each(function (index, value) {
            // without first column
            if (index > 0) { labels.push($(value).html()); }
        });
        // get target source
        var target = [];
        $.each(labels, function (index, value) {
            target.push(link.attr('data-target-source'));
        });
        // Chart initialisieren
        var modal = $(this);
        var canvas = modal.find('.modal-body canvas');
        modal.find('.modal-title').html(title);
        var data = {
            labels: ["January", "February", "March", "April", "May", "June", "July"],
            datasets: [{
                label: "My First Dataset",
                data: [65, 59, 80, 81, 56, 55, 40],
                fill: false,
                borderColor: "rgb(75, 192, 192)",
                lineTension: 0.1
            }]
        };
        var options = {};
        var ctx = canvas[0].getContext("2d");
        var chart = new Chart(ctx, {
            type: 'line',
            data: data,
            options: options
        });
    }).on('hidden.bs.modal', function (event) {
        // reset canvas size
        var modal = $(this);
        var canvas = modal.find('.modal-body canvas');
        canvas.attr('width', '568px').attr('height', '300px');
        // destroy modal
        $(this).data('bs.modal', null);
    });
});
