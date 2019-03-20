$(function () {
    eventAutocomplete();

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
});

function eventAutocomplete() {
    // https://www.devbridge.com/sourcery/components/jquery-autocomplete/
    //$('#autocomplete').autocomplete({
    //    serviceUrl: '/autocomplete/countries',
    //    onSelect: function (suggestion) {
    //        alert('You selected: ' + suggestion.value + ', ' + suggestion.data);
    //    }
    //});

    $('.autocomplete').on("focus", function () {
        $(this).autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: "/api/employees/",
                    data: {
                        type: $(this).data('type'),
                        term: request.term
                    },
                    success: function (data) {
                        response(data.outputs);
                    }
                });
            },
            minLength: 2,
            focus: function (event, ui) {
                $(this).val(ui.item.fullName);
                return false;
            },
            select: function (event, ui) {
                //$('#name').val(ui.item.name);
                return false;
            }
        }).autocomplete("instance")._renderItem = function (ul, item) {
            return $("<li>")
                .append("<div>" + item.fullName + "<br>" + item.email + " - " + item.title + "</div>")
                .appendTo(ul);
        };
    });
}
