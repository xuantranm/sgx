$(function () {
    setValue();

    if ($('#hidManagerId').val() === "") {
        shortManagerPeople();
    } else {
        $("#ManagerId").val($('#hidManagerId').val());
    }

    document.getElementById('avatar-input').addEventListener('change', loadAvatar, false);

    document.getElementById('cover-input').addEventListener('change', readCover, true);

    $('input[name="Employee.FullName"]').focusout(function (e) {
        if ($(this).val().length > 0) {
            $.ajax({
                type: "GET",
                url: "/helper/fullnamegenerate",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: { name: $(this).val() },
                success: function (data) {
                    if (data.result === false) {
                        $('.user-name-error').html(data.userName + " đã được sử dụng. Đề xuất: " + data.suggest + ",...");
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
        $('input[name="Employee.EmployeeBank.BankHolder"]').val($(this).val());
    });

    $('.btn-save-department').on('click', function () {
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
                    $('select[name="Employee.Department"] option:first').after('<option value="' + data.entity.name + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.Department"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newDepartment').modal('hide');
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

    $('.btn-save-part').on('click', function () {
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
                    $('select[name="Employee.Part"] option:first').after('<option value="' + data.entity.name + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.Part"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newPart').modal('hide');
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

    $('.btn-save-title').on('click', function () {
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
                    $('select[name="Employee.Title"] option:first').after('<option value="' + data.entity.name + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.Title"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newTitle').modal('hide');
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

    $('select[name="Employee.Part"]').on('change', function () {
        shortManagerPeople();
    });
    $('select[name="Employee.Department"]').on('change', function () {
        shortManagerPeople();
    });
    $('select[name="Employee.Title"]').on('change', function () {
        shortManagerPeople();
    });

    $('#check-timekeeper').on('change', function () {
        if ($(this).is(':checked')) {
            $('input[name="Employee.IsTimeKeeper"]').val(true);
        } else {
            $('input[name="Employee.IsTimeKeeper"]').val(false);
        }
    });

    $('#check-enable').on('change', function () {
        if ($(this).is(':checked')) {
            $('input[name="Employee.Enable"]').val(false);
            $('.leave-extend').removeClass('d-none');
        } else {
            $('input[name="Employee.Enable"]').val(true);
            $('.leave-extend').addClass('d-none');
        }
    });

    $('select[name="Employee.SalaryPayMethod"]').on('change', function () {
        //console.log($(this).val());
        if ($(this).val() === "1") {
            $('.bank-method').removeClass('d-none');
        } else {
            $('.bank-method').addClass('d-none');
        }
    });

    //js-select2-basic-single
    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
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

    $('.btn-save-hospital').on('click', function () {
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
                    $('select[name="Employee.BhxhHospital"] option:first').after('<option value="' + data.entity.name + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.BhxhHospital"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newHospital').modal('hide');

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
        $('.more-contract').before($.templates("#tmplContract").render(data));
        //setValue();
        eventContractType();
        registerDatePicker();
        enableRemove();
    });

    eventContractType();

    $('.codeSalary').each(function (i, obj) {
        enableAutoNumeric($(obj).val());
    });

    $('.addCertificate').click(function (e) {
        e.preventDefault();
        var code = 0;
        if ($('.codeCertificate')[0]) {
            code = parseInt($('.codeCertificate:last').val()) + 1;
        }

        var data = [
            {
                "code": code
            }
        ];

        $('.more-certificate').before($.templates("#tmplCertificate").render(data));
        enableAutoSize();
        enableRemove();
    });

    $('.addStorePaper').click(function (e) {
        e.preventDefault();
        var code = 0;
        if ($('.codeStorePaper')[0]) {
            code = parseInt($('.codeStorePaper:last').val()) + 1;
        }

        var data = [
            {
                "code": code
            }
        ];

        $('.more-storepaper').before($.templates("#tmplStorePaper").render(data));
        enableRemove();
    });

    $('.addEducation').click(function (e) {
        e.preventDefault();
        var code = 0;
        if ($('.codeEducation')[0]) {
            code = parseInt($('.codeEducation:last').val()) + 1;
        }
        var data = [
            {
                "code": code
            }
        ];

        $('.more-education').before($.templates("#tmplEducation").render(data));
        enableAutoSize();
        enableRemove();
    });

    enableRemove();

    $(".data-form").on("submit", function (event) {
        event.preventDefault();
        //grab all form data  
        var formData = new FormData($(this)[0]);

        // loading button
        $('#btn-save-submit').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        $('select', $('.data-form')).prop('disabled', true);
        $('textarea', $('.data-form')).prop('disabled', true);

        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('#btn-save-submit').html(loadingText);

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
                    resetForm();
                }
                else {
                    if (data.error === "user") {
                        resetForm();
                        $('input[name="Employee.UserName"]').focus();
                    }
                    if (data.error === "email") {
                        resetForm();
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

function resetForm() {
    $('#btn-save-submit').prop('disabled', false);
    $('input', $('.data-form')).prop('disabled', false);
    $('select', $('.data-form')).prop('disabled', false);
    $('textarea', $('.data-form')).prop('disabled', false);
    $('#btn-save-submit').html($('#btn-save-submit').data('original-text'));
}

function setValue() {
    $('.datepicker').each(function (i, obj) {
        var date = moment($(obj).datepicker('getFormattedDate'), 'DD-MM-YYYY')
        $('.hidedatepicker', $(obj).closest('.form-group')).val(
            date.format('MM-DD-YYYY')
        );
    });

    // load cover

    //document.getElementById('cover').style.backgroundImage = "url(" + reader.result + ")";
    //document.getElementById('cover').style.width = "357px";
    //document.getElementById('cover').style.height = "167px";
}

function eventContractType() {
    $('.contract-type').on('change', function () {
        var parent = $(this).closest('.nodeContract');
        $('.contract-type-name', parent).val($(".contract-type option:selected", parent).text());
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
            //console.log(data);
            if (data.length !== 0) {
                if (data.length > 0) {
                    var htmlEmployees = tmplManagerPeople.render(data);
                    $("#ManagerId").html(htmlEmployees);
                    $("#ManagerId").prepend($('<option value="" selected>Không có quản lý</option>'));
                    if ($('#hidManagerId').val() !== '') {
                        $("#ManagerId").val($('#hidManagerId').val());
                    }
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

function readCover() {
    var file = document.getElementById("cover-input").files[0];
    var reader = new FileReader();
    reader.onloadend = function () {
        document.getElementById('cover').style.backgroundImage = "url(" + reader.result + ")";
        document.getElementById('cover').style.width = "357px";
        document.getElementById('cover').style.height = "167px";
    }
    if (file) {
        reader.readAsDataURL(file);
    } else {
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

function enableAutoSize() {
    $('textarea.js-auto-size').textareaAutoSize();
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
