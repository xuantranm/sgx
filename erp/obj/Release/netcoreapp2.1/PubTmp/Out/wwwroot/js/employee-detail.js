$(function () {
    document.getElementById('avatar-input').addEventListener('change', loadAvatar, false);
});

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

