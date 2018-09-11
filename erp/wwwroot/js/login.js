$(function () {
    document.onkeypress = keyPress;
});

function keyPress(e) {
    var x = e || window.event;
    var key = (x.keyCode || x.which);
    if (key == 13 || key == 3) {
        //  myFunc1();
        $('form').submit();
    }
}

//$(document).ready(function () {
//    $(".data-form").on("submit", function (event) {
//        event.preventDefault();
//        //grab all form data  
//        var formData = new FormData($(this)[0]);

//        //console.log(formData);
//        var $this = $(this);
//        //var frmValues = $this.serialize();
//        $.ajax({
//            type: $this.attr('method'),
//            url: $this.attr('action'),
//            enctype: 'multipart/form-data',
//            processData: false,  // Important!
//            contentType: false,
//            data: formData
//        })
//            .done(function (data) {
//                console.log(data);
//                toastr.error("success")
//            })
//            .fail(function () {
//                toastr.error("Fail")
//            });
//        event.preventDefault();
//    });
//});