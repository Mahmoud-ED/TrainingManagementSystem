using Microsoft.AspNetCore.Mvc;

namespace TrainingManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        private readonly IWebHostEnvironment _host;

        public BaseController(IWebHostEnvironment host)
        {
            _host = host;
        }

        public string? UploadFile(string folder, IFormFile? img, string? imageUrl, string? isImg)
        {
            if (isImg == null) // في حال تم حذف الصورة فقط
            {
                DeleteOldFile(imageUrl);
                return null;
            }

            if (img != null) // في حال تم تحميل صورة جديدة
            {
                DeleteOldFile(imageUrl);

                string pictures = Path.Combine(_host.WebRootPath, "pictures");
                string fileName = $"{Guid.NewGuid()}_{img.FileName}";
                string newPath = Path.Combine(pictures, folder, fileName);

                if (!System.IO.File.Exists(newPath))
                {
                    using var stream = new FileStream(newPath, FileMode.CreateNew);
                    img.CopyTo(stream);
                }

                return fileName;
            }

            return imageUrl; // في حال لم يتم تحميل صورة جديدة تبقى الصورة القديمة كما هي
        }

        public void DeleteOldFile(string? imageUrl)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                string oldPath = Path.Combine(_host.WebRootPath, "pictures", imageUrl);
                if (System.IO.File.Exists(oldPath))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers(); // من اجل مشكلة الملف قيد الإستخدام
                    System.IO.File.Delete(oldPath);
                }
            }
        }

        public string ReadHtmlTemplate(string htmlTemplate)
        {
            string filePath = Path.Combine(_host.WebRootPath, "templates", htmlTemplate);
            using var htmlFile = new StreamReader(filePath);
            return htmlFile.ReadToEnd();
        }

        // لتفعيل التحقق من نوع الصورة، فقط فك التعليق
        /*
        public bool CheckImgExtension(IFormFile? img)
        {
            if (img != null)
            {
                string fileExtension = Path.GetExtension(img.FileName).ToLower();
                string[] validExtensions = { ".jpeg", ".jpg", ".bmp", ".gif", ".png", ".tiff", ".ico" };
                return validExtensions.Contains(fileExtension);
            }
            return true; // في حال لا يوجد ملف
        }
        */
    }
}
