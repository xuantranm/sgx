$(function () {
    eventAutocomplete();

    $('.data-form').on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);
        //console.log(formData);
        var $this = $(this);
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,  // Important!
            contentType: false,
            data: formData
        })
            .done(function (data) {
                //console.log(data);
            })
            .fail(function () {
                toastr.error(data.message)
            });
        event.preventDefault();
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
        scroll(0, parseInt(location) * 50);
        //window.location.hash = '#'+obj;
    });

    $('#sortBy').change(function (e) {
        loadData($(this).closest("form"));
    });

    $('.btn-search').on('click', function (e) {
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

function loadData(form) {
    var $this = form;
    var frmValues = $this.serialize();
    //console.log(frmValues);
    $.ajax({
        type: $this.attr('method'),
        url: $this.attr('action'),
        data: frmValues
    })
        .done(function (data) {
            //console.log(data);
            //$('#employeeList').html($.templates("#tmplDataEmployeeList").render(data.viewModel.employees));
            //toastr.info(data.message);
            ////window.location.reload();
            //setTimeout(function () {
            //    window.location.reload();
            //}, 1000);
        })
        .fail(function (data) {
            toastr.error(data.message)
        });
    event.preventDefault();
}

function OnSearch(input) {
    if (input.value === "") {
        //console.log("You either clicked the X or you searched for nothing.");
    }
    else {
        alert("You searched for " + input.value);
        var $this = $(this).closest("form");
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            data: frmValues
        })
            .done(function (data) {
                //console.log(data);
                $('#employeeList').html($.templates("#tmplDataEmployeeList").render(data.viewModel.employees));
                //toastr.info(data.message);
                ////window.location.reload();
                //setTimeout(function () {
                //    window.location.reload();
                //}, 1000);
            })
            .fail(function () {
                toastr.error(data.message)
            });
        event.preventDefault();
    }
}

function eventAutocomplete() {
    $('.aaa').on("focus", function () {
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
                $(this).val(ui.item.code);
                return false;
            },
            select: function (event, ui) {
                //$('#name').val(ui.item.name);
                return false;
            }
        }).autocomplete("instance")._renderItem = function (ul, item) {
            return $("<li>")
                .append("<div>" + item.code + "<br>" + item.fullName + "</div>")
                .appendTo(ul);
        }
    });

    //$('input[name="Search.FullName"]').autocomplete({
    //    source: function (request, response) {
    //        $.ajax({
    //            url: "/api/employees/",
    //            data: {
    //                type: 'name',
    //                term: request.term
    //            },
    //            success: function (data) {
    //                response(data.outputs);
    //            }
    //        });
    //    },
    //    minLength: 2,
    //    focus: function (event, ui) {
    //        $(this).val(ui.item.fullName);
    //        return false;
    //    },
    //    select: function (event, ui) {
    //        $('input[name="Employee.AliasFullName"]').val(ui.item.aliasFullName);
    //        return false;
    //    }
    //}).autocomplete("instance")._renderItem = function (ul, item) {
    //    return $("<li>")
    //        .append("<div>" + item.code + "<br>" + item.fullName + "</div>")
    //        .appendTo(ul);
    //};

    //$(".common-search").autocomplete({
    //    minLength: 0,
    //    source: "/helper/employeeautocomplete",
    //    focus: function (event, ui) {
    //        $(this).val(ui.item.code);
    //        return false;
    //    },
    //    select: function (event, ui) {
    //        $('.common-search').val(ui.item.code);
    //        return false;
    //    }
    //}).autocomplete("instance")._renderItem = function (ul, item) {
    //    console.log(item);
    //    if (item.avatar === null) {
    //        return $("<li>")
    //            .append("<a><img src='http://via.placeholder.com//336x336' class='avatar avatar-xs autocomplete-avatar'/>" + item.code + "</a>")
    //            .appendTo(ul);
    //    } else {
    //        console.log(item.avatar.path);
    //        return $("<li>")
    //            .append("<a><img src='" + item.avatar.path + item.avatar.fileName + "' class='avatar avatar-xs autocomplete-avatar'/>" + item.code + "</a>")
    //            .appendTo(ul);
    //    }
    //};
}
