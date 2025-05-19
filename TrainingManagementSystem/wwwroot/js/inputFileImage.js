function clickFile(file) {
    var objfile1 = document.getElementById(file);
    objfile1.click();
    return false;
}
//function UploadImageFile(input, img, isThereImg, defaultText) {
//    var file = input.files[0];
//    if (/\.(jpe?g|png|bmp|tiff|gif|ico)$/i.test(file.name.toLowerCase()) === false) {
//        alert("الملف المرفق ليس ملف صورة");
//    }
//    else {
//        if (input.files && input.files[0]) { // إذا كانت هناك ملفات وتم اختيار ملف واحد على الأقل
//            var reader = new FileReader(); //لقراءة محتويات الملفات التي تم اختيارها بواسطة عناصر إدخال الملف FileReader
//            reader.onload = function (e) {
//                document.getElementById(img).src = e.target.result; // تحتوى على البيانات التي تم قراءتها من الملف بعد نجاح عملية القراءة
//            }
//            reader.readAsDataURL(input.files[0]); // قراءة محتويات الملف المحدد وتحويلها إلى تنسيق مناسب لعرضه كمعاينة
//        }
        
//        document.getElementById(isThereImg).value = 'yes';
//        document.getElementById(defaultText).innerHTML = '';

//        {
//            //document.getElementById(isThereImg).value = file.name;
//            /*var imageUrl = document.getElementById('isThereImg');
//            imageUrl.value = 'img';*/
//            //document.getElementById("defaultText").innerHTML = null;
//            //document.getElementById(img).src = '~/img/News.png';  // لا يعمل؟
//            //alert(input.files[0]); // file(0)
//            //alert(input.files); // file list
//        }
//    }
//}
function UploadImageFile(input, img, isThereImg, defaultText) {
    var file = input.files[0];

    if (!file) {
        alert("Please select a file.");
        return;
    }

    // Check if the file is an image
    if (!/\.(jpe?g|png|bmp|tiff|gif|ico)$/i.test(file.name.toLowerCase())) {
        alert("The attached file is not an image.");
        return;
    }

    // If file is valid, process it
    if (file && input.files && input.files[0]) {
        var reader = new FileReader(); // Create a FileReader instance

        // When the file is loaded, set the image preview
        reader.onload = function (e) {
            var imgElement = document.getElementById(img);
            if (imgElement) {
                imgElement.src = e.target.result; // Set the source to the uploaded image
            }
        };

        // Read the file as a Data URL (image preview)
        reader.readAsDataURL(input.files[0]);
    }

    // Ensure the isThereImg and defaultText elements exist before modifying them
    var isThereImgElement = document.getElementById(isThereImg);
    var defaultTextElement = document.getElementById(defaultText);

    if (isThereImgElement) {
        isThereImgElement.value = 'yes'; // Indicate that an image has been uploaded
    }

    if (defaultTextElement) {
        defaultTextElement.innerHTML = ''; // Clear the default text (if any)
    }
}

function clearFile(input, img, isThereImg) {
    //alert("test1");
    document.getElementById(input).value = null;
    document.getElementById(img).src = '';
    document.getElementById(isThereImg).value = null;
    return false; //إذا كان الزر داخل نموذج، فإنه  سيمنع عادةً إرسال النموذج
}


// كتابة اسم الملف فقط نستعمله في حال الملف المرفوع ليس صورة /
$(document).ready(function () {
    $('.custom-file-input').on("change", function () {
        var fileName = $(this).val().split("\\").pop();
        $(this).next('.custom-file-label').html(fileName);
    });
});
