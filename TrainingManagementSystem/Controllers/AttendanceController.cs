using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
[ViewLayout("_LayoutDashboard")]
[Authorize(Roles = "Admin,Prog")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Attendance/Index?courseDetailsId={id}
    // شاشة العرض الرئيسية (المصفوفة) لحضور دورة محددة
    public async Task<IActionResult> Index(Guid? courseDetailsId)
    {
        if (courseDetailsId == null)
        {
            // اختياري: يمكنك عرض قائمة بالدورات ليختار منها المستخدم
            return View("SelectCourse"); // إنشاء View لعرض الدورات المتاحة
        }

        // جلب تفاصيل الدورة مع اسم الدورة الرئيسي
        var courseDetails = await _context.CourseDetails
            .Include(cd => cd.Course) // لجلب اسم الدورة من جدول Course
            .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);

        if (courseDetails == null)
        {
            return NotFound("لم يتم العثور على الدورة المحددة.");
        }

        // جلب المتدربين المسجلين في هذه الدورة عبر جدول CourseTrainee
        var trainees = await _context.CourseTrainees
            .Where(ct => ct.CourseDetailsId == courseDetailsId)
            .Include(ct => ct.Trainee) // لجلب معلومات المتدرب
            .Select(ct => new TraineeInfo // استخدام ViewModel خفيف
            {
                Id = ct.Trainee.Id,
                Name = ct.Trainee.ArName
            })
            .OrderBy(t => t.Name)
            .ToListAsync();

        // تحديد نطاق التواريخ من الدورة نفسها
        var startDate = courseDetails.StartDate.Date;
        // إذا لم يكن هناك تاريخ انتهاء، افترض أنه اليوم أو بعد 30 يومًا
        var endDate = courseDetails.EndDate?.Date ?? DateTime.Today;

        // جلب كل سجلات الحضور لهذه الدورة ضمن نطاق التواريخ
        var attendanceRecords = await _context.Attendances
            .Where(a => a.CourseDetailsId == courseDetailsId && a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .ToListAsync();

        // تحويل السجلات إلى Dictionary لسهولة الوصول في الـ View (لأداء أفضل)
        var attendanceDictionary = attendanceRecords
            .ToDictionary(a => (a.TraineeId, a.AttendanceDate.Date), a => a);

        var model = new CourseAttendanceIndexViewModel
        {
            CourseDetailsId = courseDetails.Id,
            CourseName = courseDetails.Course.Name, // استخدام اسم الدورة الرئيسي
            Trainees = trainees,
            DatesInRange = Enumerable.Range(0, (endDate - startDate).Days + 1).Select(d => startDate.AddDays(d)).ToList(),
            AttendanceRecords = attendanceDictionary
        };

        return View(model);
    }

    // GET: /Attendance/DailyAttendance?courseDetailsId={id}&date={date}
    // عرض شاشة إدخال الحضور ليوم واحد
    [HttpGet]
    public async Task<IActionResult> DailyAttendance(Guid courseDetailsId, DateTime? date)
    {
        var attendanceDate = date?.Date ?? DateTime.Today;

        var courseDetails = await _context.CourseDetails
            .Include(cd => cd.Course)
            .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);

        if (courseDetails == null) return NotFound();

        // جلب قائمة المتدربين المسجلين في الدورة
        var trainees = await _context.CourseTrainees
           .Where(ct => ct.CourseDetailsId == courseDetailsId)
           // ملاحظة: لم نعد بحاجة إلى .Include() هنا
           .Select(ct => new TraineeInfo // أو أي ViewModel آخر تستخدمه
           {
               Id = ct.Trainee.Id,
               Name = ct.Trainee.ArName
           })
           .OrderBy(t => t.Name)
           .ToListAsync();

        // جلب السجلات الموجودة مسبقاً لهذا اليوم لتعبئة النموذج
        var existingRecords = await _context.Attendances
            .Where(a => a.CourseDetailsId == courseDetailsId && a.AttendanceDate == attendanceDate)
            .ToDictionaryAsync(a => a.TraineeId, a => a);

        var model = new DailyAttendanceViewModel
        {
            CourseDetailsId = courseDetailsId,
            CourseName = courseDetails.Course.Name,
            AttendanceDate = attendanceDate,
            TraineeItems = trainees.Select(t => new TraineeAttendanceItem
            {
                TraineeId = t.Id,
                TraineeName = t.Name,
                // إذا وجد سجل سابق، استخدمه. وإلا، افترض "غائب" كقيمة افتراضية
                Status = existingRecords.ContainsKey(t.Id) ? existingRecords[t.Id].Status : AttendanceStatus.Absent,
                Notes = existingRecords.ContainsKey(t.Id) ? existingRecords[t.Id].Notes : null
            }).ToList()
        };

        return View(model);
    }

    // POST: /Attendance/DailyAttendance
    // حفظ سجلات الحضور ليوم واحد (دفعة واحدة)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DailyAttendance(DailyAttendanceViewModel model)
    {
        //if (!ModelState.IsValid)
        //{
        //    // إعادة تعبئة اسم الدورة لأنه ليس جزءًا من النموذج المرسل
        //    var course = await _context.Courses.FindAsync(model.CourseDetailsId);
        //    model.CourseName = course?.Name;
        //    return View(model);
        //}

        // جلب السجلات الحالية من قاعدة البيانات لتحديد ما إذا كنا سنقوم بـ INSERT أو UPDATE
        var existingRecords = await _context.Attendances
            .Where(a => a.CourseDetailsId == model.CourseDetailsId && a.AttendanceDate == model.AttendanceDate)
            .ToListAsync();

        foreach (var item in model.TraineeItems)
        {
            var record = existingRecords.FirstOrDefault(r => r.TraineeId == item.TraineeId);
            if (record != null) // السجل موجود، قم بالتحديث
            {
                record.Status = item.Status;
                record.Notes = item.Notes;
                _context.Update(record);
            }
            else // السجل غير موجود، قم بالإضافة
            {
                var newRecord = new Attendance
                {
                    //Id = Guid.NewGuid(), // BaseEntity قد يعتني بهذا
                    CourseDetailsId = model.CourseDetailsId,
                    TraineeId = item.TraineeId,
                    AttendanceDate = model.AttendanceDate,
                    Status = item.Status,
                    Notes = item.Notes
                };
                _context.Add(newRecord);
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"تم حفظ سجل الحضور ليوم {model.AttendanceDate:yyyy-MM-dd} بنجاح.";
        return RedirectToAction("Index", new { courseDetailsId = model.CourseDetailsId });
    }

    // GET: /Attendance/PrintableSheet?courseDetailsId={id}
    // عرض كشف ورقي فارغ للطباعة
    public async Task<IActionResult> PrintableSheet(Guid courseDetailsId)
    {
        var courseDetails = await _context.CourseDetails
            .Include(cd => cd.Course)
            .Include(cd => cd.CoursDetailsTrainer).ThenInclude(cdt => cdt.Trainer) // جلب
                                                                                   // 
            .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);

        if (courseDetails == null) return NotFound();

        var trainees = await _context.CourseTrainees
            .Where(ct => ct.CourseDetailsId == courseDetailsId)
            .Select(ct => ct.Trainee)
            .Select(t => new TraineeInfo { Id = t.Id, Name = t.ArName })
            .OrderBy(t => t.Name)
            .ToListAsync();

        // تجميع أسماء المدربين في نص واحد
        var instructorNames = string.Join(" / ", courseDetails.CoursDetailsTrainer.Select(cdt => cdt.Trainer.ArName));

        var model = new PrintableSheetViewModel
        {
            CourseName = courseDetails.Course.Name,
            InstructorName = instructorNames,
            PrintDate = DateTime.Today,
            Trainees = trainees
        };

        return View(model);
    }
}