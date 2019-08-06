$(function () {
    setValue();
    var optionsborn = {
        // Thanh pho
        types: ['(cities)'],
        // phuong
        //types: ["(regions)"]
        componentRestrictions: { country: 'vn' }
    };
    var bornplace = new google.maps.places.Autocomplete($("#Employee_Bornplace")[0], { componentRestrictions: { country: 'vn' } });
    google.maps.event.addListener(bornplace, 'place_changed', function () {
        var place = bornplace.getPlace();
        //console.log(place.address_components);
    });

    var addressResident = new google.maps.places.Autocomplete($("#Employee_AddressResident")[0], { componentRestrictions: { country: 'vn' } });
    google.maps.event.addListener(addressResident, 'place_changed', function () {
        var place = addressResident.getPlace();
        //console.log(place.address_components);
    });

    var addressTemporary = new google.maps.places.Autocomplete($("#Employee_AddressTemporary")[0], {});
    google.maps.event.addListener(addressTemporary, 'place_changed', function () {
        var place = addressTemporary.getPlace();
        //console.log(place.address_components);
    });

    $('.avatar-current').on('click', function () {
        if ($('#avatar-name-1').val() !== "") {
            $('#avatarShow').attr("src", $('#avatar-path-1').val() + $('#avatar-name-1').val());
        }
    });

    $('.avatar-new').on('click', function () {
        if ($('#avatar-name-2').val() !== "") {
            $('#avatarShow').attr("src", $('#avatar-path-2').val() + $('#avatar-name-2').val());
        }
        $(this).addClass('d-none');
        $('.confirm-avatar-cancel').removeClass('d-none');
        $('#Employee_Avatar_Path').val($('#avatar-path-2').val());
        $('#Employee_Avatar_FileName').val($('#avatar-name-2').val());
        $('#Employee_Avatar_OrginalName').val($('#avatar-orginal-2').val());
    });

    $('.confirm-avatar-cancel').on('click', function () {
        if ($('#avatar-name-1').val() !== "") {
            $('#avatarShow').attr("src", $('#avatar-path-1').val() + $('#avatar-name-1').val());
        }
        else {
            $('#avatarShow').attr("src", window.location.origin + "/images/placeholder/120x120.png");
        }
        $(this).addClass('d-none');
        $('.avatar-new').removeClass('d-none');
        $('#Employee_Avatar_Path').val('');
        $('#Employee_Avatar_FileName').val('');
        $('#Employee_Avatar_OrginalName').val('');
    });

    $('.btn-change-data').on('click', function () {
        $('input', $(this).closest('.form-group')).val($(this).data('value'));
    });

    $('.select-change-data').on('click', function () {
        $('select', $(this).closest('.form-group')).val($(this).data('value'));
    });

    $('.select2-change-data').on('click', function () {
        $('select', $(this).closest('.form-group')).val($(this).data('value')).trigger('change');
    });

    $('.txt-change-data').on('click', function () {
        $('textarea', $(this).closest('.form-group')).val($(this).data('value'));
    });

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
                            $('input[name="Employee.UserName"]').val(data.userName);
                            $('input[name="Employee.Email"]').val(data.email);
                        }
                    }
                }
            });
        }
        $('input[name="Employee.EmployeeBank.BankHolder"]').val($(this).val());
    });

    //$('#Employee_CongTyChiNhanh').on('change', function () {
    //    $('#Employee_CongTyChiNhanhName').val($('#Employee_CongTyChiNhanh option:selected').text());
    //    changeByCongTyChiNhanh($(this).val());
    //});

    $('#Employee_KhoiChucNang').on('change', function () {
        $('#Employee_KhoiChucNangName').val($('#Employee_KhoiChucNang option:selected').text());
        changeByKhoiChucNang($(this).val());
    });

    $('#Employee_PhongBan').on('change', function () {
        $('#Employee_PhongBanName').val($('#Employee_PhongBan option:selected').text());
        changeByPhongBan($(this).val());
    });

    $('#Employee_BoPhan').on('change', function () {
        $('#Employee_BoPhanName').val($('#Employee_BoPhan option:selected').text());
        changeByBoPhan($(this).val());
    });

    $('#Employee_BoPhanCon').on('change', function () {
        $('#Employee_BoPhanConName').val($('#Employee_BoPhanCon option:selected').text());
    });

    $('#newPhongBanModal').on('show.bs.modal', function (event) {
        var modal = $(this);
        modal.find('.CongTyChiNhanhIdModal').val($('select[name="Employee.CongTyChiNhanh"]').val());
        modal.find('.KhoiChucNangIdModal').val($('select[name="Employee.KhoiChucNang"]').val());
        eventModal(modal, "phongban");
    });

    $('#newKhoiChucNangModal').on('show.bs.modal', function (event) {
        var modal = $(this);
        eventModal(modal, "khoichucnang");
    });

    $('#newChucVuModal').on('show.bs.modal', function (event) {
        var modal = $(this);
        eventModal(modal, "chucvu");
    });

    $('.btn-save-khoichucnang').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues,
            success: function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    $('select[name="Employee.KhoiChucNang"] option:first').after('<option value="' + data.entity.id + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.KhoiChucNang"]').val(data.entity.id);
                    $('select[name="Employee.KhoiChucNangName"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newKhoiChucNangModal').modal('hide');
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });

    $('.btn-save-phongban').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues,
            success: function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    // Update ddl
                    $('select[name="Employee.PhongBan"] option:first').after('<option value="' + data.entity.id + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.PhongBan"]').val(data.entity.id);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newPhongBanModal').modal('hide');
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });

    $('.btn-save-chucvu').on('click', function () {
        var form = $(this).closest('form');
        var frmValues = form.serialize();
        $.ajax({
            type: form.attr('method'),
            url: form.attr('action'),
            data: frmValues,
            success: function (data) {
                if (data.result === true) {
                    toastr.info(data.message);
                    $('select[name="Employee.ChucVu"] option:first').after('<option value="' + data.entity.id + '">' + data.entity.name + '</option>');
                    $('select[name="Employee.ChucVu"]').val(data.entity.id);
                    $('select[name="Employee.ChucVuName"]').val(data.entity.name);
                    $('input', form).val('');
                    $('textarea', form).val('');
                    $('#newChucVuModal').modal('hide');
                }
                else {
                    toastr.error(data.message);
                }
            }
        });
    });

    $('#check-timekeeper').on('change', function () {
        if ($(this).is(':checked')) {
            $('input[name="Employee.IsTimeKeeper"]').val(true);
            $('.time-working').addClass('d-none');
        } else {
            $('input[name="Employee.IsTimeKeeper"]').val(false);
            $('.time-working').removeClass('d-none');
        }
    });

    $('.chkOfficial').on('change', function () {
        if ($(this).is(':checked')) {
            $('#Employee_Official').val(true);
        } else {
            $('#Employee_Official').val(false);
        }
    });

    $('#check-enable').on('change', function () {
        if ($(this).is(':checked')) {
            $('#Employee_Leave').val(true);
        } else {
            $('#Employee_Leave').val(false);
        }
    });

    $('.chkWelcomeEmailSend').on('change', function () {
        if ($(this).is(':checked')) {
            $('#IsWelcomeEmail').val(true);
            $('.welcomeEmail').removeClass('d-none');
            var phongban = $('#Employee_PhongBan').val();
            if (phongban === "") {
                $('.welcomeToEmailList').text("Chưa chọn phòng ban! Danh sách email này dựa vào phòng ban.");
            }
            else {
                $.ajax({
                    type: "GET",
                    url: "/api/GetWelcomeToEmails",
                    contentType: "application/json; charset=utf-8",
                    data: { PhongBan: $('#Employee_PhongBan').val() },
                    dataType: "json",
                    success: function (data) {
                        if (data.result === true) {
                            $('.welcomeToEmailList').text("TO: " + data.tohtml);
                            $('.welcomeCCEmailList').text("CC: " + data.cchtml);
                        }
                    }
                });
            }
        } else {
            $('#IsWelcomeEmail').val(false);
            $('.welcomeEmail').addClass('d-none');
        }
    });

    $('.chkLeaveEmailSend').on('change', function () {
        if ($(this).is(':checked')) {
            $('#IsLeaveEmail').val(true);
            $('.leaveEmail').removeClass('d-none');
            var phongban = $('#Employee_PhongBan').val();
            if (phongban === "") {
                $('.welcomeToEmailList').text("Chưa chọn phòng ban! Danh sách email này dựa vào phòng ban.");
            }
            else {
                $.ajax({
                    type: "GET",
                    url: "/api/GetWelcomeToEmails", // dung chung
                    contentType: "application/json; charset=utf-8",
                    data: { PhongBan: $('#Employee_PhongBan').val() },
                    dataType: "json",
                    success: function (data) {
                        if (data.result === true) {
                            $('.leaveToEmailList').text("TO: " + data.tohtml);
                            $('.leaveCCEmailList').text("CC: " + data.cchtml);
                        }
                    }
                });
            }
        } else {
            $('#IsLeaveEmail').val(false);
            $('.leaveEmail').addClass('d-none');
        }
    });

    $('.chkWelcomeEmailAll').on('change', function () {
        if ($(this).is(':checked')) {
            $('#WelcomeEmailAll').val(true);
            $.ajax({
                type: "GET",
                url: "/api/GetWelcomeToEmails",
                contentType: "application/json; charset=utf-8",
                data: { PhongBan: '', All: true },
                dataType: "json",
                success: function (data) {
                    console.log(data.cchtml);
                    if (data.result === true) {
                        $('.welcomeToEmailList').text("TO: " + data.tohtml);
                        $('.welcomeCCEmailList').text("CC: " + data.cchtml);
                    }
                }
            });
        } else {
            $('#WelcomeEmailAll').val(false);
        }
    });

    $('.chkLeaveEmailAll').on('change', function () {
        if ($(this).is(':checked')) {
            $('#LeaveEmailAll').val(true);
            $.ajax({
                type: "GET",
                url: "/api/GetWelcomeToEmails",
                contentType: "application/json; charset=utf-8",
                data: { PhongBan: '', All: true },
                dataType: "json",
                success: function (data) {
                    console.log(data.cchtml);
                    if (data.result === true) {
                        $('.leaveToEmailList').text("TO: " + data.tohtml);
                        $('.leaveCCEmailList').text("CC: " + data.cchtml);
                    }
                }
            });
        } else {
            $('#LeaveEmailAll').val(false);
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

    $('.js-select2-basic-single').select2(
        {
            theme: "bootstrap"
        });

    $('.datepicker').on('changeDate', function () {
        var date = moment($(this).datepicker('getFormattedDate'), 'DD-MM-YYYY');
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
            data: frmValues,
            success: function (data) {
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
            }
        });
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
        if (!confirm("Bạn chắc chắn thông tin đã được kiểm tra và muốn cập nhật!")) {
            return false;
        }

        if ($('#IsWelcomeEmail').val() === "true") {
            if (!confirm("Bạn đã chọn <Gửi email thông báo nhân sự mới> !! Hệ thống sẽ gửi email thông báo tới tất cả nhân viên công ty, cc cho các cấp lãnh đạo. Chọn hủy/cancel và bỏ dấu <Gửi email thông báo nhân sự mới> nếu không muốn gửi email.")) {
                return false;
            }
            else {
                // Load content email:...
                // Do later...
            }
        }

        if ($('#IsLeaveEmail').val() === "true") {
            if (!confirm("Bạn đã chọn <Gửi email thông báo nhân sự nghỉ việc> !! Hệ thống sẽ gửi email thông báo tới tất cả nhân viên công ty, cc cho các cấp lãnh đạo. Chọn hủy/cancel và bỏ dấu <Gửi email thông báo nhân sự nghỉ việc> nếu không muốn gửi email.")) {
                return false;
            }
            else {
                // Load content email:...
                // Do later...
            }
        }

        // add value dropdownlist name
        $('#Employee_CongTyChiNhanhName').val($('#Employee_CongTyChiNhanh option:selected').text());
        $('#Employee_KhoiChucNangName').val($('#Employee_KhoiChucNang option:selected').text());
        $('#Employee_PhongBanName').val($('#Employee_PhongBan option:selected').text());
        $('#Employee_BoPhanName').val($('#Employee_BoPhan option:selected').text());
        $('#Employee_BoPhanConName').val($('#Employee_BoPhanCon option:selected').text());
        $('#Employee_ChucVuName').val($('#Employee_ChucVu option:selected').text());

        event.preventDefault();
        var formData = new FormData($(this)[0]);
        $('#btn-save-submit').prop('disabled', true);
        $('input', $('.data-form')).prop('disabled', true);
        $('select', $('.data-form')).prop('disabled', true);
        $('textarea', $('.data-form')).prop('disabled', true);
        var loadingText = '<i class="fas fa-spinner"></i> đang xử lý...';
        $('#btn-save-submit').html(loadingText);
        var $this = $(this);
        $.ajax({
            type: $this.attr('method'),
            url: $this.attr('action'),
            enctype: 'multipart/form-data',
            processData: false,  // Important!
            contentType: false,
            data: formData,
            success: function (data) {
                if (data.result === true) {
                    toastr.success(data.message);
                    setTimeout(function () {
                        window.location = "/";
                    }, 1000);
                }
                else {
                    if (data.source === "user") {
                        resetForm();
                        $('input[name="Employee.UserName"]').focus();
                    }
                    if (data.source === "email") {
                        resetForm();
                        $('input[name="Employee.Email"]').focus();
                    }
                    toastr.error(data.message);
                }
            }
        });
    });

    function changeByCongTyChiNhanh(congtychinhanh) {
        $.ajax({
            type: "GET",
            url: "/api/GetByCongTyChiNhanh",
            contentType: "application/json; charset=utf-8",
            data: { congtychinhanh: congtychinhanh },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $kcn = $("#Employee_KhoiChucNang");
                    $kcn.empty();
                    if (data.khoichucnangs.length > 1) {
                        $kcn.append($("<option></option>")
                            .attr("value", "").text("Chọn"));
                    }
                    $.each(data.khoichucnangs, function (key, khoichucnang) {
                        $kcn.append($("<option></option>")
                            .attr("value", khoichucnang.id).text(khoichucnang.name));
                    });

                    if (data.khoichucnangs.length === 1) {
                        changeByKhoiChucNang($('#Employee_KhoiChucNang').val());
                    }

                    var $pb = $("#Employee_PhongBan");
                    $pb.empty();

                    var $bp = $("#Employee_BoPhan");
                    $bp.empty();

                    var $bpc = $("#Employee_BoPhanCon");
                    $bpc.empty();
                }

                getChucVu($('#Employee_CongTyChiNhanh').val(), $('#Employee_KhoiChucNang').val(), $("#Employee_PhongBan").val(), $("#Employee_BoPhan").val());
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
                    $('#Employee_CongTyChiNhanh').val(data.congTyChiNhanhId);

                    var $pb = $("#Employee_PhongBan");
                    $pb.empty();
                    if (data.phongbans.length > 1) {
                        $pb.append($("<option></option>")
                            .attr("value", "").text("Chọn"));
                    }
                    $.each(data.phongbans, function (key, phongban) {
                        $pb.append($("<option></option>")
                            .attr("value", phongban.id).text(phongban.name));
                    });

                    if (data.phongbans.length === 1) {
                        changeByPhongBan($('#Employee_PhongBan').val());
                    }

                    var $bp = $("#Employee_BoPhan");
                    $bp.empty();

                    var $bpc = $("#Employee_BoPhanCon");
                    $bpc.empty();
                }

                getChucVu($('#Employee_CongTyChiNhanh').val(), $('#Employee_KhoiChucNang').val(), $("#Employee_PhongBan").val(), $("#Employee_BoPhan").val());
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
                    var $bp = $("#Employee_BoPhan");
                    $bp.empty();
                    $.each(data.bophans, function (key, bophan) {
                        $bp.append($("<option></option>")
                            .attr("value", bophan.id).text(bophan.name));
                    });

                    var $bpc = $("#Employee_BoPhanCon");
                    $bpc.empty();

                    var $manager = $("#ManagerId");
                    $manager.empty();
                    $manager.append($("<option></option>")
                        .attr("value", "").text("Chọn"));
                    $.each(data.managers, function (key, manager) {
                        var display = manager.name;
                        if (manager.employee !== null) {
                            display += " ( " + manager.employee + " )";
                        }
                        $manager.append($("<option></option>")
                            .attr("value", manager.id).text(display));
                    });
                }

                getChucVu($('#Employee_CongTyChiNhanh').val(), $('#Employee_KhoiChucNang').val(), $("#Employee_PhongBan").val(), $("#Employee_BoPhan").val());
            }
        });
    }

    function changeByBoPhan(bophan) {
        $.ajax({
            type: "GET",
            url: "/api/GetByBoPhan",
            contentType: "application/json; charset=utf-8",
            data: { bophan: bophan },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $pbc = $("#Employee_BoPhanCon");
                    $pbc.empty();
                    $.each(data.bophancons, function (key, bophancon) {
                        $pbc.append($("<option></option>")
                            .attr("value", bophancon.id).text(bophancon.name));
                    });
                }
            }
        });
    }

    function getChucVu(congtychinhanh, khoichucnang, phongban, bophan) {
        $.ajax({
            type: "GET",
            url: "/api/GetChucVu",
            contentType: "application/json; charset=utf-8",
            data: {
                congtychinhanh: congtychinhanh,
                khoichucnang: khoichucnang,
                phongban: phongban,
                bophan: bophan
            },
            dataType: "json",
            success: function (data) {
                if (data.result === true) {
                    var $pbc = $("#Employee_ChucVu");
                    $pbc.empty();
                    $.each(data.chucvus, function (key, chucvu) {
                        $pbc.append($("<option></option>")
                            .attr("value", chucvu.id).text(chucvu.name));
                    });
                }
            }
        });
    }

});

function resetForm() {
    $('#btn-save-submit').prop('disabled', false);
    $('input', $('.data-form')).prop('disabled', false);
    $('select', $('.data-form')).prop('disabled', false);
    $('textarea', $('.data-form')).prop('disabled', false);
    $('#btn-save-submit').html('Lưu');
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
    };
    if (file) {
        reader.readAsDataURL(file);
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

function enableAutoSize() {
    $('textarea.js-auto-size').textareaAutoSize();
}

function eventModal(modal, mode) {
    $('.CongTyChiNhanhIdModal').on('change', function () {
        changeByCongTyChiNhanhModal(modal, mode, $(this).val());
    });
    $('.KhoiChucNangIdModal').on('change', function () {
        changeByKhoiChucNangModal(modal, mode, $(this).val());
    });
    $('.PhongBanIdModal').on('change', function () {
        changeByPhongBanModal(modal, mode, $(this).val());
    });
    //if (mode === "chucvu") {
    //    dataChucVuModal(modal, "", "", $('.PhongBanIdModal', modal).val());
    //}
}

function changeByCongTyChiNhanhModal(modal, mode, congtychinhanh) {
    $.ajax({
        type: "GET",
        url: "/api/GetByCongTyChiNhanh",
        contentType: "application/json; charset=utf-8",
        data: { congtychinhanh: congtychinhanh },
        dataType: "json",
        success: function (data) {
            if (data.result === true) {
                var $kcn = $(".KhoiChucNangIdModal", modal);
                $kcn.empty();
                $kcn.append($("<option></option>")
                    .attr("value", "").text("Chọn"));
                $.each(data.khoichucnangs, function (key, khoichucnang) {
                    $kcn.append($("<option></option>")
                        .attr("value", khoichucnang.id).text(khoichucnang.name));
                });
                if (mode === "phongban") {
                    if (data.khoichucnangs.length === 1) {
                        dataPhongBanModal(modal);
                    }
                }
                else {
                    //var $pb = $(".PhongBanIdModal", modal);
                    //$pb.empty();
                }
            }
        }
    });
}

function changeByKhoiChucNangModal(modal, mode, khoichucnang) {
    $.ajax({
        type: "GET",
        url: "/api/GetByKhoiChucNang",
        contentType: "application/json; charset=utf-8",
        data: { khoichucnang: khoichucnang },
        dataType: "json",
        success: function (data) {
            if (data.result === true) {
                if (mode === "khoichucnang") {
                    dataKhoiChucNangModal(congtychinhanh);
                }
                else {
                    var $pb = $('.PhongBanIdModal', modal);
                    $pb.empty();
                    $pb.append($("<option></option>")
                        .attr("value", "").text("Chọn"));
                    $.each(data.phongbans, function (key, phongban) {
                        $pb.append($("<option></option>")
                            .attr("value", phongban.id).text(phongban.name));
                    });
                }
            }
        }
    });
}

function changeByPhongBanModal(modal, mode, phongban) {
    dataChucVuModal(modal, "", "", phongban);
}

function dataKhoiChucNangModal(modal, congtychinhanh) {
    $.ajax({
        type: "GET",
        url: "/api/GetKhoiChucNang",
        contentType: "application/json; charset=utf-8",
        data: {
            congtychinhanh: congtychinhanh
        },
        dataType: "json",
        success: function (data) {
            if (data.result === true) {
                var $khoichucnang = $('.existData', modal);
                $khoichucnang.empty();
                $.each(data.khoichucnangs, function (key, khoichucnang) {
                    $khoichucnang.append("<small class='badge badge-primary'>" + khoichucnang.name + "</small>");
                });
            }
        }
    });
}

function dataPhongBanModal(modal, khoichucnang) {
    $.ajax({
        type: "GET",
        url: "/api/GetPhongBan",
        contentType: "application/json; charset=utf-8",
        data: {
            khoichucnang: khoichucnang
        },
        dataType: "json",
        success: function (data) {
            if (data.result === true) {
                var $phongban = $('.existData', modal);
                $phongban.empty();
                $.each(data.phongbans, function (key, phongban) {
                    $phongban.append("<small class='badge badge-primary'>" + phongban.name + "</small>");
                });
            }
        }
    });
}

function dataChucVuModal(modal, congtychinhanh, khoichucnang, phongban) {
    $.ajax({
        type: "GET",
        url: "/api/GetChucVu",
        contentType: "application/json; charset=utf-8",
        data: {
            congtychinhanh: congtychinhanh,
            khoichucnang: khoichucnang,
            phongban: phongban
        },
        dataType: "json",
        success: function (data) {
            if (data.result === true) {
                var $chucvu = $('.existData', modal);
                $chucvu.empty();
                $.each(data.chucvus, function (key, chucvu) {
                    $chucvu.append("<small class='badge badge-primary'>" + chucvu.name + "</small>");
                });
            }
        }
    });
}