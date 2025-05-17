$(document).ready(function () {
    // Örnek cümle ekleme
    $("#addSample").click(function () {
        var sampleHtml = `
            <div class="input-group mb-2">
                <span class="input-group-text"><i class="fas fa-quote-right"></i></span>
                <input type="text" name="samples[]" class="form-control" placeholder="Enter an example sentence" />
                <button type="button" class="btn btn-outline-danger remove-sample">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `;
        $("#samplesContainer").append(sampleHtml);
    });

    // Örnek cümle silme
    $(document).on("click", ".remove-sample", function () {
        if ($("#samplesContainer .input-group").length > 1) {
            $(this).closest(".input-group").remove();
        } else {
            $(this).closest(".input-group").find("input").val('');
        }
    });

    // Resim önizleme
    $("#pictureInput").change(function () {
        if (this.files && this.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $("#picturePreview").attr("src", e.target.result).removeClass("d-none");
                $("#emptyPicture").addClass("d-none");
            }
            reader.readAsDataURL(this.files[0]);
        }
    });

    // Resim temizleme
    $("#clearPicture").click(function () {
        $("#pictureInput").val('');
        $("#picturePreview").addClass("d-none").attr("src", "");
        $("#emptyPicture").removeClass("d-none");
    });

    // Ses önizleme
    $("#audioInput").change(function () {
        if (this.files && this.files[0]) {
            var url = URL.createObjectURL(this.files[0]);
            $("#audioSource").attr("src", url);
            $("#audioPlayer").removeClass("d-none");
            $("#emptyAudio").addClass("d-none");
            $("audio")[0].load();
        }
    });

    // Ses temizleme
    $("#clearAudio").click(function () {
        $("#audioInput").val('');
        $("#audioSource").attr("src", "");
        $("#audioPlayer").addClass("d-none");
        $("#emptyAudio").removeClass("d-none");
    });
});