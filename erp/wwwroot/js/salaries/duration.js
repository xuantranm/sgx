$(function () {
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('#Thang').on('change', function (e) {
        formSubmit();
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
                        window.location.reload();
                    }, 1000);
                }
                else {
                    toastr.error(data.message);
                    $('.btn-submit').prop('disabled', false);
                    $('input', $('.data-form')).prop('disabled', false);
                    //$('select', $('.data-form')).prop('disabled', false);
                    //$('textarea', $('.data-form')).prop('disabled', false);
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });

    $('.ddl-salary').on('change', function () {
        var parent = $(this).closest('form');
        if ($(this).val() !== "") {
            $('.salary-month', parent).val($('option:selected', this).attr('data-month'));
            $('.salary-year', parent).val($('option:selected', this).attr('data-year'));
        } else {
            $('.salary-month', parent).val(0);
            $('.salary-year', parent).val(0);
        }
    });

    $('.ddl-logistics').on('change', function () {
        var parent = $(this).closest('form');
        if ($(this).val() !== "") {
            $('.logistics-month', parent).val($('option:selected', this).attr('data-month'));
            $('.logistics-year', parent).val($('option:selected', this).attr('data-year'));
        } else {
            $('.logistics-month', parent).val(0);
            $('.logistics-year', parent).val(0);
        }
    });

    $('.ddl-sale').on('change', function () {
        var parent = $(this).closest('form');
        if ($(this).val() !== "") {
            $('.sale-month', parent).val($('option:selected', this).attr('data-month'));
            $('.sale-year', parent).val($('option:selected', this).attr('data-year'));
        } else {
            $('.sale-month', parent).val(0);
            $('.sale-year', parent).val(0);
        }
    });

    //$('.btn-edit').on('click', function () {
    //    console.log($(this).attr("data-id"));
    //    var trlosest = $(this).closest("tr");
    //    console.log($('.salary-item', trlosest).html());
    //    console.log($('.logistic-item', trlosest).html());
    //    console.log($('.sale-item', trlosest).html());
    //    $('tr').removeClass('active');
    //    $(trlosest).addClass('active');
    //    var formupdate = $('#update-duration');
    //    $(formupdate).focus();
    //    $(formupdate).addClass('show');
    //    //$('#SalaryDuration.Id')
    //});
});


