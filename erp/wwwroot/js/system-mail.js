$(function () {
    var $table = $('table.floating-header');
    $table.floatThead();

    $('.checkbox-all').click(function (event) {
        if (this.checked) {
            $('.checkbox-item').each(function () {
                this.checked = true;
                $('.status-item', $(this).closest('td')).val(3);
            });
        } else {
            $('.checkbox-item').each(function () {
                this.checked = false;
                $('.status-item', $(this).closest('td')).val(0);
            });
        }
    });

    $('.checkbox-item').each(function () {
        $(this).click(function () {
            if (this.checked) {
                $('.status-item', $(this).closest('td')).val(3);
            } else {
                $('.status-item', $(this).closest('td')).val(0);
            }
        });
    });

    $(".data-form").on("submit", function (event) {
        if (!$('input[type=checkbox]:checked').length) {
            toastr.error("Chọn email để gửi lại.");
            //stop the form from submitting
            return false;
        }

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
                    $('.btn-submit').html($('.btn-submit').data('original-text'));
                }
            })
            .fail(function () {
                toastr.error("Error.");
            });
    });

});

