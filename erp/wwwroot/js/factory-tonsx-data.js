$(function () {
    setValueFactory();

    tondaungayAutoNumeric = new AutoNumeric('.tondaungay', { decimalPlaces: 2, allowDecimalPadding: false, readOnly: true  });
    toncuoingayAutoNumeric = new AutoNumeric('.toncuoingay', { decimalPlaces: 2, allowDecimalPadding: false, readOnly: true  }); 

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(this).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });

    $('select[name="Entity.ProductId"]').on('change', function () {
        // load information
        $.ajax({
            type: 'get',
            url: '/api/factory/product-infomation',
            data: {
                product: $(this).val()
            }
        })
            .done(function (data) {
                if (data.result === true) {
                    var entity = data.outputs;
                    $('input[name="Entity.Unit"]').val(entity.unit);
                    $('input[name="Entity.TonDauNgay"]').val(entity.tonCuoiNgay);
                    tondaungayAutoNumeric.set(entity.tonCuoiNgay);
                    updateTonCuoiNgay();
                }
            });
    });

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

    $('.autonumeric').each(function (i, obj) {
        enableFactoryAutoNumeric($(obj).data("id"));
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
                toastr.error(data.message)
            });
        event.preventDefault();
    });
});

function setValueFactory() {
    $('.datepicker').each(function (i, obj) {
        var date = moment($(obj).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(obj).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });
}

function enableFactoryAutoNumeric(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.autonumeric-' + code, { decimalPlaces: 2, allowDecimalPadding: false });

    $('.autonumeric-' + code).on('keyup', function () {
        var trueVal = Number(newInstall.getNumericString());
        $('.autonumeric-value', $(this).closest('.form-group')).val(trueVal);
        updateTonCuoiNgay();
    });
}

function updateTonCuoiNgay() {
    var result = 0;
    var input = 0;
    var output = 0;
    var tondaungay = NanValue(parseFloat($('input[name="Entity.TonDauNgay"]').val()));
    var nhaptusanxuat = NanValue(parseFloat($('input[name="Entity.NhapTuSanXuat"]').val()));
    var nhaptukho = NanValue(parseFloat($('input[name="Entity.NhapTuKho"]').val()));
    var xuatchosanxuat = NanValue(parseFloat($('input[name="Entity.XuatChoSanXuat"]').val()));
    var xuatchokho = NanValue(parseFloat($('input[name="Entity.XuatChoKho"]').val()));
    var xuathaohut = NanValue(parseFloat($('input[name="Entity.XuatHaoHut"]').val()));
    var taixuat = NanValue(parseFloat($('input[name="Entity.TaiXuat"]').val()));
    var tainhap = NanValue(parseFloat($('input[name="Entity.TaiNhap"]').val()));
    input = tondaungay + nhaptusanxuat + nhaptukho + tainhap;
    output = xuatchokho + xuatchosanxuat + xuathaohut + taixuat;
    result = parseFloat(input) - parseFloat(output);
    $('input[name="Entity.TonCuoiNgay"]').val(result);
    toncuoingayAutoNumeric.set(result);
}


