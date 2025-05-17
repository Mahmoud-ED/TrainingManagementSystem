namespace TrainingManagementSystem.Classes
{
    public class FunctionsHelper
    {
        public static bool CheckImgExtension(IFormFile? img)
        {
            if (img != null)
            {
                string fileExtension = Path.GetExtension(img.FileName.ToLower());
                string[] validExtensions = { ".jpeg", ".jpg", ".bmp", ".gif", ".png", ".tiff", ".ico" };
                if (validExtensions.Contains(fileExtension))
                    return true;
                else
                    return false;
            }
            return true; /// في حال لا يوجد ملف
        }

        public static byte[] FileToByte(IFormFile file)
        {
            var target = new MemoryStream();
            file.CopyTo(target);
            return target.ToArray();
        }

    }
}
