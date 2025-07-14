using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

public class TestConnectionController : Controller
{
    public IActionResult Index()
    {
        string message;
        try
        {
            using (var conn = new SqlConnection("Server=102.213.180.7;Database=TMS;User Id=TmsUser;Password=Tms+User;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;"))
            {
                conn.Open();
                message = "✅ تم الاتصال بقاعدة البيانات بنجاح!";
            }
        }
        catch (Exception ex)
        {
            message = "❌ فشل الاتصال: " + ex.Message;
        }

        // إرجاع النتيجة في الـ View
        ViewBag.Message = message;
        return View();
    }
}
