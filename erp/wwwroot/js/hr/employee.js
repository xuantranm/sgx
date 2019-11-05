$(function () {
    eventAutocomplete();

    $('.btn-chart-save').on('click', function () {
        $('#chart-config').collapse('hide');
        loadChart();
    });

    loadChart();

    $(".delete-item").on("click", function (event) {
        var id = $(this).attr("data-id");
        if (confirm("Bạn muốn xóa dữ liệu này!")) {
            $.ajax({
                type: "GET",
                url: "/api/EmployeeDisable",
                contentType: "application/json; charset=utf-8",
                data: {
                    Id: id
                },
                dataType: "json",
                success: function (data) {
                    if (data.result === true) {
                        toastr.success(data.message);
                        setTimeout(function () {
                            location.reload();
                        }, 1000);
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.ddlEmployeeId').on('change', function () {
        formSubmit();
    });

    $('#Sortby').on('change', function () {
        formSubmit();
    });

    $('.ctcn').on('click', function () {
        var obj = $(this).data('alias');
        if ($('#' + obj).hasClass('d-none')) {
            $('.ctcn-items').addClass('d-none');
            $('.icon-mark').removeClass('icon-chevron-up').addClass('icon-chevron-down');

            $('#' + obj).removeClass('d-none');
            $('.icon-mark', $('.ctcn-' + obj)).removeClass('icon-chevron-down').addClass('icon-chevron-up');
        } else {
            $('#' + obj).addClass('d-none');
            $('icon-mark', $('.ctcn-' + obj)).removeClass('icon-chevron-up').addClass('icon-chevron-down');
        }
    });

    $('.kcn').on('click', function () {
        var obj = $(this).data('alias');
        if ($('#' + obj).hasClass('d-none')) {
            $('.kcn-items').addClass('d-none');
            $('.icon-mark').removeClass('icon-chevron-up').addClass('icon-chevron-down');

            $('#' + obj).removeClass('d-none');
            $('.icon-mark', $('.kcn-' + obj)).removeClass('icon-chevron-down').addClass('icon-chevron-up');
        } else {
            $('#' + obj).addClass('d-none');
            $('.icon-mark', $('.kcn-' + obj)).removeClass('icon-chevron-up').addClass('icon-chevron-down');
        }
    });

    $('.pb').on('click', function () {
        var obj = $(this).data('alias');
        if ($('#' + obj).hasClass('d-none')) {
            $('.pb-items').addClass('d-none');
            $('.pb-icon').removeClass('icon-chevron-up').addClass('icon-chevron-down');

            $('#' + obj).removeClass('d-none');
            $('.icon-mark', $('.pb-' + obj)).removeClass('icon-chevron-down').addClass('icon-chevron-up');
        } else {
            $('#' + obj).addClass('d-none');
            $('.icon-mark', $('.pb-' + obj)).removeClass('icon-chevron-up').addClass('icon-chevron-down');

        }
    });

    $('.bp').on('click', function () {
        var obj = $(this).data('alias');
        if ($('#' + obj).hasClass('d-none')) {
            $('.bp-items').addClass('d-none');
            $('.bp-icon').removeClass('icon-chevron-up').addClass('icon-chevron-down');

            $('#' + obj).removeClass('d-none');
            $('.icon-mark', $('.bp-' + obj)).removeClass('icon-chevron-down').addClass('icon-chevron-up');
        } else {
            $('#' + obj).addClass('d-none');
            $('.icon-mark', $('.bp-' + obj)).removeClass('icon-chevron-up').addClass('icon-chevron-down');
        }
    });

    //$('#Sortby').change(function (e) {
    //    loadData($(this).closest("form"));
    //});

    $.views.converters({
        date: function (val) {
            var dateVal = new Date(val);
            var condition = new Date("1900");
            if (dateVal < condition) {
                return "N/A";
            }
            return moment(val).format("DD/MM/YYYY HH:mm");
        },
        born: function (val) {
            var dateVal = new Date(val);
            var condition = new Date("1900");
            if (dateVal < condition) {
                return "N/A";
            }
            return moment(val).format("DD/MM/YYYY");
        }
    });

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
