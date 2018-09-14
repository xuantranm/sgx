$(function () {
    eventAutocomplete();

    $('#sortBy').on('change', function () {
        $("#form-search").submit();
    });

    $('.part').on('click', function () {
        var obj = $(this).data('alias');
        var location = $(this).data('order');
        if ($('#' + obj).hasClass('d-none')) {
            $('.part-items').addClass('d-none');
            $('.part-icon').removeClass('icon-chevron-up').addClass('icon-chevron-down');
            $('#' + obj).removeClass('d-none');
            $('i', $('.part-' + obj)).removeClass('icon-chevron-down').addClass('icon-chevron-up');
        } else {
            $('i', $('.part-' + obj)).removeClass('icon-chevron-up').addClass('icon-chevron-down');
            $('#' + obj).addClass('d-none');
        }
        //scroll(0, parseInt(location) * 50);
        //window.location.hash = '#'+obj;
    });

    $('#sortBy').change(function (e) {
        loadData($(this).closest("form"));
    });

    $.views.converters({
        date: function (val) {
            var dateVal = new Date(val);
            var condition = new Date("1900");
            if (dateVal < condition) {
                return "N/A";
            }
            return moment(val).format("DD/MM/YYYY HH:mm")
        },
        born: function (val) {
            var dateVal = new Date(val);
            var condition = new Date("1900");
            if (dateVal < condition) {
                return "N/A";
            }
            return moment(val).format("DD/MM/YYYY")
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
                $(this).val(ui.item.email);
                return false;
            },
            select: function (event, ui) {
                //$('#name').val(ui.item.name);
                return false;
            }
        }).autocomplete("instance")._renderItem = function (ul, item) {
            return $("<li>")
                .append("<div>" + item.email + "<br>" + item.fullName + "</div>")
                .appendTo(ul);
        }
    });

    
}
