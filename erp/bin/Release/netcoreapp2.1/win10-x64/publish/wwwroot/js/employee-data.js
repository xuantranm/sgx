$(function () {
    setValue();

    document.getElementById('avatar-input').addEventListener('change', loadAvatar, false);

    $('input[name="Employee.FullName"]').focusout(function (e) {
        if ($(this).val().length > 0) {
            $.ajax({
                type: "GET",
                url: "/helper/fullnamegenerate",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: { name: $(this).val() },
                success: function (data) {
                    //console.log(data);
                    if (data.result === false) {
                        //console.log("Error.");
                        //console.log(data);
                    }
                    else {
                        if (data.length !== 0) {
                            $('input[name*="UserName"]').val(data.userName);
                            $('input[name*="Email"]').val(data.email);
                        }
                    }
                }
            });
        }
        $('input[name="Employee.EmployeeBank.BankHolder"]').val(S(this).val());
    });

    $('select[name="Employee.Part"]').on('change', function () {
        shortManagerPeople();
    });
    $('select[name="Employee.Department"]').on('change', function () {
        shortManagerPeople();
    });
    $('select[name="Employee.Title"]').on('change', function () {
        shortManagerPeople();
    });

    $('select[name="Employee.SalaryPayMethod"]').on('change', function () {
        console.log($(this).val());
        if ($(this).val() === "1") {
            $('.bank-method').removeClass('d-none');
        } else {
            $('.bank-method').addClass('d-none');
        }
    });
    $('.btn-submit').prop('disabled', false);

    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(this).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });

    $('.multi-select-workplace').multiselect({
        //enableFiltering: true,
        nonSelectedText: '',
        nSelectedText: '',
        allSelectedText: '',
        //onChange: function (option, checked) {
        //    alert('onChange!');
        //},
        onDropdownHide: function (event) {
            var workplaces = $('.multi-select-workplace').val();
            $('.nodeWorkplace').remove();
            //$('.nodeWorkplace').addClass('d-none');
            var i = 0;
            workplaces.forEach(function (item) {
                //if (item === "VP") {
                //    $('.van-phong').removeClass('d-none');
                //}
                //if (item === "NM") {
                //    $('.nha-may').removeClass('d-none');
                //}

                var data = [
                    {
                        "code": i,
                        "nameWorkplace": $(".multi-select-workplace option[value='" + item + "']").text(),
                        "codeWorkplace": item,
                    }
                ];
                $('.workplace').after($.templates("#tmplWorkplace").render(data));
                i++;
            });
        }
    });

    $('.data-form input').each(function () {
        var elem = $(this);
        // Save current value of element
        elem.data('oldVal', elem.val());

        // Look for changes in the value
        elem.bind("propertychange change click keyup input paste", function (event) {
            // If value has changed...
            if (elem.data('oldVal') !== elem.val()) {
                // Updated stored value
                elem.data('oldVal', elem.val());
                // Do action
                $('.btn-submit').prop('disabled', false);
            }
        });
    });

    $('#public').on('change', function () {
        if ($(this).is(':checked')) {
            $('input[name*="BhxhEnable"]').val(true);
            $('.enableBhxh').removeClass('d-none');
        } else {
            $('input[name*="BhxhEnable"]').val(false);
            $('.enableBhxh').addClass('d-none');
        }
    });

    $('.addMobile').click(function (e) {
        // Can remove > 1 element
        e.preventDefault();
        var code = 0;
        if ($('.codeMobile')[0]) {
            code = parseInt($('.codeMobile:last').val()) + 1;
        }

        var data = [
            {
                "code": code
            }
        ];
        $('.more-mobile').before($.templates("#tmplMobile").render(data));
        $('.remove-item', $('.nodeMobile')).removeClass('d-none');
        enableRemoveMobileExist();
    });

    $('.addBHXH').click(function (e) {
        e.preventDefault();
        var code = 0;
        if ($('.codeBHXH')[0]) {
            code = parseInt($('.codeBHXH:last').val()) + 1;
        }

        var data = [
            {
                "code": code
            }
        ];
        
        $('.more-task-bhxh').before($.templates("#tmplBHXH").render(data));
        registerDatePicker();
        enableRemove();
    });

    $('.task-bhxh').on('change', function () {
        $('.task-bhxh-display', $(this).closest('.node')).val($(this).find("option:selected").text());
    });

    $('.addFamily').click(function (e) {
        e.preventDefault();
        var code = 0;
        if ($('.codeFamily')[0]) {
            code = parseInt($('.codeFamily:last').val()) + 1;
        }

        var data = [
            {
                "code": code
            }
        ];

        $('.more-family').before($.templates("#tmplFamily").render(data));
        //$('.nodeFamily:last').after($.templates("#tmplFamily").render(data));
        registerDatePicker();
        enableRemove();
    });

    $('.addContract').click(function (e) {
        e.preventDefault();
        var code = 0;
        if ($('.codeContract')[0]) {
            code = parseInt($('.codeContract:last').val()) + 1;
        }

        var data = [
            {
                "code": code
            }
        ];
        //$('.nodeContract:last').after($.templates("#tmplContract").render(data));
        $('.more-contract').before($.templates("#tmplContract").render(data));
        setValue();
        registerDatePicker();
        enableRemove();
    });

    //$('.addSalary').click(function (e) {
    //    e.preventDefault();
    //    var code = 0;
    //    if ($('.codeSalary')[0]) {
    //        code = parseInt($('.codeSalary:last').val()) + 1;
    //    }
    //    var data = [
    //        {
    //            "code": code
    //        }
    //    ];
    //    $('.more-salary').before($.templates("#tmplSalary").render(data));
    //    enableAutoNumeric(code);
    //    enableRemove();
    //});

    $('.codeSalary').each(function (i, obj) {
        enableAutoNumeric($(obj).val());
    });

    enableRemove();

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
                    $('#modalnotice').html($.templates("#tmplModalNotice").render(data));
                }
                else {
                    if (data.error === "user") {
                        $('input[name="Employee.UserName"]').focus();
                    }
                    if (data.error === "email") {
                        $('input[name="Employee.Email"]').focus();
                    }
                    toastr.error(data.message);
                }
            })
            .fail(function () {
                toastr.error(data.message)
            });
        event.preventDefault();
    });
});

function setValue() {
    $('.datepicker').each(function (i, obj) {
        var date = moment($(obj).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(obj).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });
}

function shortManagerPeople() {
    var tmplManagerPeople = $.templates("#tmplManagePeople");
    $.ajax({
        url: '/api/employee-filter',
        type: 'GET',
        data: {
            part: $('select[name="Employee.Part"]').val(),
            department: $('select[name="Employee.Department"]').val(),
            //title: $('select[name="Employee.Title"]').val()
        },
        datatype: 'json',
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            console.log(data);
            if (data.length !== 0) {
                if (data.length > 0) {
                    var htmlEmployees = tmplManagerPeople.render(data);
                    $("#ManagerId").html(htmlEmployees);
                    $("#ManagerId").prepend($('<option value="" selected>Chọn</option>'));
                }
            }
        }
    });
}

function loadAvatar(evt) {
    var files = evt.target.files; // FileList object
    // Loop through the FileList and render image files as thumbnails.
    for (var i = 0, f; f = files[i]; i++) {

        // Only process image files.
        if (!f.type.match('image.*')) {
            continue;
        }

        var reader = new FileReader();

        // Closure to capture the file information.
        reader.onload = (function (theFile) {
            return function (e) {
                // Render thumbnail.
                document.getElementById('avatarShow').src = e.target.result;
                document.getElementById('avatarShow').title = escape(theFile.name);

            };
        })(f);

        // Read in the image file as a data URL.
        reader.readAsDataURL(f);
    }
}

function enableRemoveMobileExist() {
    $('.remove-item').on('click', function (e) {
        $(this).closest('.node').remove();
        // Hide remove button if only 1 item
        if ($('.nodeMobile').length === 1) {
            $('.remove-item', $('.nodeMobile')).addClass('d-none');
        }
    });
}

function enableAutoNumeric(code) {
    var newInstall = code;
    newInstall = new AutoNumeric('.autonumeric-' + code, { decimalPlaces: 0 });

    $('.autonumeric-'+ code).on('keyup', function () {
        var trueVal = Number(newInstall.getNumericString());
        $('.moneytostring', $(this).closest('.form-group')).html(capitalizeFirstLetter(num2Word2.convert(trueVal)));
        $('.autonumeric-value', $(this).closest('.form-group')).val(trueVal);

        updateTotalEmployeeSalary();
    });

    
    //$('.autonumeric').on('keyup', function () {

    //    var trueVal = Number(autoNumericInstance.getNumericString());

    //    $('.moneytostring', $(this).closest('.form-group')).html(DocTienBangChu(trueVal));
    //    $('.autonumeric-value', $(this).closest('.form-group')).val(autoNumericInstance.getNumericString());
    //});
}

function displayMoney(element) {
    var trueVal = element.val().replace(/\,/g, '');
    trueVal = Number(trueVal);
    //console.log(DocTienBangChu(trueVal));
}

function updateTotalEmployeeSalary() {
    var result = 0;
    $('.salary-item-money').each(function (i, obj) {
        result += parseFloat($(obj).val());
    });

    //var textResult = accounting.formatNumber(result);
    var textResult = accounting.formatMoney(result, "VNĐ ", 0);
    $('#salary-estimate').val(textResult);
}
