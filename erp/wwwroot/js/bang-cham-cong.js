$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.left-menu').addClass('d-none');

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlphong').on('change', function () {
        formSubmit();
    });

    $('.ddlEmployeeId').on('change', function () {
        formSubmit();
    });

    $('#thang').on('change', function (e) {
        formSubmit();
    });

    $('.from-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.tu-ngay').val(
            date.format('MM-DD-YYYY')
        );
    });

    $('.to-date').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
        $('.den-ngay').val(
            date.format('MM-DD-YYYY')
        );
    });

    tangca = new AutoNumeric('.form-control-tang-ca', { decimalPlaces: 2 });

    $('.xac-nhan-tang-ca-form').on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);
        // loading button
        $('.btn-xac-nhan-tang-ca', $(this)).prop('disabled', true);
        $('.btn-khong-xac-nhan-tang-ca', $(this).closest('.card')).prop('disabled', true);
        $('input', $(this)).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-xac-nhan-tang-ca', $(this)).html(loadingText);
        var $this = $(this);
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,
            contentType: false,
            data: formData,
            success: function (data) {
                // ajax later
                console.log(data);
                formSubmit();
            }
        });
    });

    $('.khong-xac-nhan-tang-ca-form').on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);
        // loading button
        $('.btn-xac-nhan-tang-ca', $(this).closest('.card')).prop('disabled', true);
        $('.btn-khong-xac-nhan-tang-ca', $(this)).prop('disabled', true);
        $('input', $(this)).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-khong-xac-nhan-tang-ca', $(this)).html(loadingText);
        var $this = $(this);
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,
            contentType: false,
            data: formData,
            success: function (data) {
                // ajax later
                console.log(data);
                formSubmit();
            }
        });
    });
});


