$(function () {
    setValueDateFormat();

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#xe').on('select2:select', function (e) {
        formSubmit();
    });

    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(this).closest('.date-area')).val(
            date.format('MM-DD-YYYY')
        );
        formSubmit();
    });
});

function printdiv(printpage) {
    var headstr = "<html><head><title></title></head><body>";
    var footstr = "</body>";
    var newstr = document.all.item(printpage).innerHTML;
    var oldstr = document.body.innerHTML;
    document.body.innerHTML = headstr + newstr + footstr;
    window.print();
    document.body.innerHTML = oldstr;
    return false;
}

function PrintElem(elem) {
    var mywindow = window.open('', 'PRINT', 'height=400,width=800');

    mywindow.document.write('<html><head><title>' + document.title + '</title>');
    mywindow.document.write('</head><body >');
    mywindow.document.write(document.getElementById(elem).innerHTML);
    mywindow.document.write('</body></html>');

    mywindow.document.close(); // necessary for IE >= 10
    mywindow.focus(); // necessary for IE >= 10*/

    mywindow.print();
    mywindow.close();

    return true;
}