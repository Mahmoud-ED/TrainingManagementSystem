using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Models;

namespace TrainingManagementSystem.Controllers
{
    public class PrintController : Controller
    {
        private readonly ApplicationDbContext _context; // استبدل ApplicationDbContext باسم الـ DbContext الخاص بك

        public PrintController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        public async Task<IActionResult> PrintCertificate(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("معرف التسجيل مطلوب.");
            }

            var IsPased = await _context.CourseTrainees.AnyAsync(ct => ct.Id == id && ct.Grade != null);
            if (!IsPased)
            {
                return BadRequest("لا يمكن طباعة الشهادة لأن المتدرب لم يحصل على درجة.");
            }
            //var courseTrainee = await _context.CourseTrainees
            //    .Include(ct => ct.CourseDetails) // لجلب تفاصيل تنفيذ الدورة
            //        .ThenInclude(cd => cd.Course) // لجلب اسم الدورة الرئيسي وبياناتها
            //    .Include(ct => ct.CourseDetails)
            //        .ThenInclude(cd => cd.Locations) // إذا كان الموقع جدول منفصل
            //    .FirstOrDefaultAsync(ct => ct.Id == id);


            var courseTrainee = await _context.CourseTrainees
    .Include(ct => ct.Trainee)
    .Include(ct => ct.CourseDetails)
        .ThenInclude(cd => cd.Course) // جلب الدورة من تفاصيل الدورة
    .Include(ct => ct.CourseDetails)
        .ThenInclude(cd => cd.Locations) // جلب الموقع من تفاصيل الدورة
    .FirstOrDefaultAsync(ct => ct.Id == id);

            if (courseTrainee == null)
            {
                return NotFound("بيانات التسجيل غير موجودة.");
            }

            if (courseTrainee.CourseDetails == null || courseTrainee.CourseDetails.Course == null)
            {
                // هذا يعني أن بعض البيانات المرتبطة مفقودة، يجب معالجة هذا
                return BadRequest("بيانات مرتبطة بالدورة أو المتدرب مفقودة.");
            }



            var viewModel = new CertificateViewModel
            {
                TraineeName = courseTrainee.Trainee.ArName, // افترض أن لديك خاصية FullName في Trainee
                CourseName = courseTrainee.CourseDetails.Course.Name,
                CourseStartDate = courseTrainee.CourseDetails.StartDate,
                CourseEndDate = courseTrainee.CourseDetails.EndDate,
                CourseDurationHours = courseTrainee.CourseDetails.DurationHours,
                CourseNumberoftargets = courseTrainee.CourseDetails.Numberoftargets,
                CourseLocation = courseTrainee.CourseDetails.Locations?.Name ?? "غير محدد", // افترض أن Location له Name
                CertificateNumber = courseTrainee.CertificateNumber,
                CertificateIssueDate = courseTrainee.CertificateIssueDate ?? DateTime.Now, // تاريخ اليوم كافتراضي إذا لم يحدد
            };

            return View("PrintCertificate", viewModel);
        }
    }
}
