$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    registerAutoNumeric();

    $('#CongTyChiNhanh').on('change', function () {
        changeByCongTyChiNhanh($(this).val());
    });

    $('#KhoiChucNang').on('change', function () {
        changeByKhoiChucNang($(this).val());
    });

    $('#PhongBan').on('change', function () {
        changeByPhongBan($(this).val());
    });

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        var $this = $(this);
        var frmValues = $this.serialize();

        // loading button
        $('.btn-submit').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('.btn-submit').html(loadingText);

        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            data: frmValues
        })
            .done(function (data) {
                if (data.result === true) {
                    toastr.success(data.message);
                    setTimeout(function () {
                        window.location.replace(data.url);
                    }, 1000);
                }
                else {
                    toastr.error(data.message);
                    $('.btn-submit').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
                $('.btn-submit').prop('disabled', false);
                $('input', $('.data-form')).prop('disabled', false);
                $('.btn-submit').html($('.btn-submit').data('original-text'));
            });
    });

    function registerAutoNumeric() {
        // No need set true value. When submit lib AutoNumeric auto generate true value.
        mucluong = new AutoNumeric('.muc-luong', { decimalPlaces: 0 });
        heso = new AutoNumeric('.he-so', { decimalPlaces: 2 });

        //$('.codeHeSo').each(function (i, obj) {
        //    enableHeSoReal($(obj).val());
        //});

        //$('.codeMucLuong').each(function (i, obj) {
        //    enableMucLuongReal($(obj).val());
        //});

        //$('.toithieuvungdoanhnghiepapdung').on('keyup', function () {
        //    var money = $(this).val();
        //    //update diem tham khao
        //    $('.mucluongthamkhao').each(function (index, currentElement) {
        //        if (parseInt($(currentElement).val().replace(/,/g, '')) < parseInt(money.replace(/,/g, ''))) {
        //            $(currentElement).val(money);
        //        }
        //    });
        //    calculatorThangLuong('', 0, money);
        //});
    }

    function enableHeSoReal(code) {
        var newInstall = code;
        newInstall = new AutoNumeric('.heso-' + code, { decimalPlaces: 2, allowDecimalPadding: false });

        $('.heso-' + code).on('keyup', function () {
            var heso = $(this).val();
            var id = $('.id-' + code).val();
            var money = $('.mucluong-' + code).val();
            calculatorThangLuong(id, heso, money);
        });
    }

    function enableMucLuongReal(code) {
        var newInstall = code;
        newInstall = new AutoNumeric('.mucluong-' + code, { decimalPlaces: 2, allowDecimalPadding: false });

        $('.mucluong-' + code).on('keyup', function () {
            var heso = $('.heso-' + code).val();
            var id = $('.id-' + code).val();
            var money = $(this).val();
            calculatorThangLuong(id, heso, money);
        });
    }

    function calculatorThangLuong(id, heso, money) {
        $.ajax({
            type: 'get',
            url: $('#hidCalculatorThangBangLuongReal').val(),
            data: {
                id: id,
                heso: heso,
                money: money
            }
        })
            .done(function (data) {
                if (data.result === true) {
                    $.each(data.list, function (index, value) {
                        $('.mucluongresult-' + value.id).html(accounting.formatNumber(value.money));
                    });
                }
            });
    }

    function changeByCongTyChiNhanh(congtychinhanh) {
        $.ajax({
            type: "GET",
            url: "/api/GetByCongTyChiNhanh",
            contentType: "application/json; charset=utf-8",
            data: { congtychinhanh: congtychinhanh },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $kcn = $("#KhoiChucNang");
                    $kcn.empty();
                    $kcn.append($("<option></option>")
                        .attr("value", "").text("Chọn"));
                    $.each(data.khoichucnangs, function (key, khoichucnang) {
                        $kcn.append($("<option></option>")
                            .attr("value", khoichucnang.id).text(khoichucnang.name));
                    });

                    if (data.khoichucnangs.length === 1) {
                        changeByKhoiChucNang($('#KhoiChucNang').val());
                    }

                    var $pb = $("#PhongBan");
                    $pb.empty();

                    var $bp = $("#BoPhan");
                    $bp.empty();
                }
            }
        });
    }

    function changeByKhoiChucNang(khoichucnang) {
        $.ajax({
            type: "GET",
            url: "/api/GetByKhoiChucNang",
            contentType: "application/json; charset=utf-8",
            data: { khoichucnang: khoichucnang },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    $('#CongTyChiNhanh').val(data.congTyChiNhanhId);

                    var $pb = $("#PhongBan");
                    $pb.empty();
                    $pb.append($("<option></option>")
                        .attr("value", "").text("Chọn"));
                    $.each(data.phongbans, function (key, phongban) {
                        $pb.append($("<option></option>")
                            .attr("value", phongban.id).text(phongban.name));
                    });

                    if (data.phongbans.length === 1) {
                        changeByPhongBan($('#PhongBan').val());
                    }

                    var $bp = $("#BoPhan");
                    $bp.empty();
                }
            }
        });
    }

    function changeByPhongBan(phongban) {
        $.ajax({
            type: "GET",
            url: "/api/GetByPhongBan",
            contentType: "application/json; charset=utf-8",
            data: { phongban: phongban },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $bp = $("#BoPhan");
                    $bp.empty();
                    $bp.append($("<option></option>")
                        .attr("value", "").text("Chọn"));
                    $.each(data.bophans, function (key, bophan) {
                        $bp.append($("<option></option>")
                            .attr("value", bophan.id).text(bophan.name));
                    });
                }
            }
        });
    }
});
