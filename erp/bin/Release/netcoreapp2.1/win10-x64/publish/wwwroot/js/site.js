// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function () {
    loadCommonData();;

    $(".datepicker").datepicker({
        language: "vi",
        format: 'dd/mm/yyyy',
    });

    $('.multi-select').multiselect({
        //enableFiltering: true,
        nonSelectedText: '',
        nSelectedText: '',
        allSelectedText: ''
    });

    $('.btn-save-pwd').on('click', function () {
        // check password
        var check = true;
        if ($('#newpassword').val().trim().length < 0) {
            toastr.error("Mật khẩu không để trống, khoảng cách.");
            check = false;
        }
        if ($('#newpassword').val().trim() !== $('#renewpassword').val().trim()) {
            toastr.error("[Nhập lại Mật khẩu mới] không khớp. Vui lòng kiểm tra.");
            check = false;
        }

        if (check) {
            $.ajax({
                type: 'post',
                url: '/api/updatepwd?newpassword=' + $('#newpassword', $('.data-form-change-pwd')).val(),
                processData: false,
                contentType: false,
            })
                .done(function (data) {
                    if (data.result === true) {
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                    $('#pwdModal').modal('hide');
                })
                .fail(function () {
                    toastr.error(data.message)
                });
        }
    });
    // http://autonumeric.org/configurator
    // Initialization
    // No declare common. error if not exist element


    // Use jQuery UI datepicker
    //$(".datepicker").datepicker({
    //    showOtherMonths: true,
    //    selectOtherMonths: true
    //});


    //https://uxsolutions.github.io/bootstrap-datepicker/?markup=range&format=dd%2Fmm%2Fyyyy&weekStart=&startDate=&endDate=&startView=0&minViewMode=0&maxViewMode=4&todayBtn=false&clearBtn=false&language=vi&orientation=auto&multidate=&multidateSeparator=&daysOfWeekHighlighted=0&daysOfWeekHighlighted=6&keyboardNavigation=on&forceParse=on#sandbox
    //$('.datepicker').datepicker({
    //    language: "vi",
    //    format: 'dd/mm/yyyy',
    //    daysOfWeekHighlighted: "0,6",
    //    todayHighlight: true,
    //    onSelect: function (dateText, inst) {
    //        console.log(dateText);
    //        console.log(inst);
    //        $('.hidedatepicker', $(this).closest('.form-group')).val(dateText);
    //    }
    //    //startDate: '-0d'
    //});

    //$('.input-daterange').datepicker({
    //    language: "vi",
    //    //format: 'dd/mm/yyyy',
    //    daysOfWeekHighlighted: "0,6",
    //    todayHighlight: true,
    //    startDate: '-0d'
    //});

    //$('.datetimepicker').datetimepicker({
    //    locale: 'vi'
    //});

    //$('#datetimepicker4').datetimepicker();

    //$('.datetimepickerfrom').datetimepicker();
    //$('.datetimepickerto').datetimepicker({
    //    useCurrent: false //Important! See issue #1075
    //});
    //$(".datetimepickerfrom").on("dp.change", function (e) {
    //    $('.datetimepickerto').data("DateTimePicker").minDate(e.date);
    //});
    //$(".datetimepickerto").on("dp.change", function (e) {
    //    $('.datetimepickerfrom').data("DateTimePicker").maxDate(e.date);
    //});

    $('textarea.js-auto-size').textareaAutoSize();

    $("#more").on("hide.bs.collapse", function () {
        $(".custom-more").html('<i class="icon icon-add-to-list"></i> Mở rộng');
    });
    $("#more").on("show.bs.collapse", function () {
        $(".custom-more").html('<i class="icon icon-list"></i> Thu gọn');
    });

    fixImg();
    $(window).on('resize', function () {
        fixImg();
    });

    function fixImg() {
        if ($(window).width() < 575.98) {
            $('.content-body img').css({ 'width': '100%' });
        }
    }    

    toastr.options = {
        preventDuplicates : true
    };
});

function loadCommonData() {
    //var tmpl = $.templates("#activityTmpl");
    $.ajax({
        type: "GET",
        url: "/helper/ErpCommon",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            //console.log(data);
            if (data.result === false) {
                //console.log("Error.");
                //console.log(data);
            }
            else {
                if (data.length !== 0) {
                    //console.log(data.userInformation);
                    $('.card-info').html($.templates("#tmplCardInfo").render(data.userInformation));
                    $('#ownerInfo li').html($.templates("#tmplOwnerInfo").render(data.ownerInformation));
                }
            }
        }
    });
}

function enableRemove() {
    $('.remove-item').on('click', function (e) {
        $(this).closest('.node').remove();
    });
}

function registerDatePicker() {
    $(".datepicker").datepicker({
        language: "vi",
        format: 'dd/mm/yyyy',
    });
    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(this).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });
}

const num2Word2 = function () {
    var t = ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"],
        r = function (r, n) {
            var o = "",
                a = Math.floor(r / 10),
                e = r % 10;
            return a > 1 ? (o = " " + t[a] + " mươi", 1 == e && (o += " mốt")) : 1 == a ? (o = " mười", 1 == e && (o += " một")) : n && e > 0 && (o = " lẻ"), 5 == e && a >= 1 ? o += " lăm" : 4 == e && a >= 1 ? o += " tư" : (e > 1 || 1 == e && 0 == a) && (o += " " + t[e]), o
        },
        n = function (n, o) {
            var a = "",
                e = Math.floor(n / 100),
                n = n % 100;
            return o || e > 0 ? (a = " " + t[e] + " trăm", a += r(n, !0)) : a = r(n, !1), a
        },
        o = function (t, r) {
            var o = "",
                a = Math.floor(t / 1e6),
                t = t % 1e6;
            a > 0 && (o = n(a, r) + " triệu", r = !0);
            var e = Math.floor(t / 1e3),
                t = t % 1e3;
            return e > 0 && (o += n(e, r) + " ngàn", r = !0), t > 0 && (o += n(t, r)), o
        };
    return {
        convert: function (r) {
            if (0 == r) return t[0];
            var n = "",
                a = "";
            do ty = r % 1e9, r = Math.floor(r / 1e9), n = r > 0 ? o(ty, !0) + a + n : o(ty, !1) + a + n, a = " tỷ"; while (r > 0);
            return n.trim()
        }
    }
}();

//String.prototype.capitalize = function () {
//    return this.charAt(0).toUpperCase() + this.slice(1);
//}
function capitalizeFirstLetter(string) {
    return string[0].toUpperCase() + string.slice(1);
}