using AutoMapper; // إذا كنت تستخدم AutoMapper للتحويل بين Entity و ViewModel
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity; // لـ UserManager إذا كنت تتعامل مع ApplicationUser
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models; // ApplicationDbContext
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels;
using System.IO; // For Path

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    public class TraineeController : BaseController
    {
        private readonly ApplicationDbContext _context; // للوصول المباشر إذا لزم الأمر
        private readonly IUnitOfWork<Trainee> _traineeUoW;
        private readonly IUnitOfWork<Organizition> _organizationUoW;
        private readonly IUnitOfWork<Department> _departmentUoW;
        private readonly IUnitOfWork<Specialization> _specializationUoW;
        private readonly IUnitOfWork<Qualification> _qualificationUoW;
        private readonly IUnitOfWork<CourseTrainee> _courseTraineeUoW; // لإدارة تسجيل الدورات
        private readonly UserManager<ApplicationUser> _userManager; // إذا كنت تربط المتدربين بحسابات مستخدمين

        public TraineeController(
            ApplicationDbContext context,
            IUnitOfWork<Trainee> traineeUoW,
            IUnitOfWork<Organizition> organizationUoW,
            IUnitOfWork<Department> departmentUoW,
            IUnitOfWork<Specialization> specializationUoW,
            IUnitOfWork<Qualification> qualificationUoW,
            IUnitOfWork<CourseTrainee> courseTraineeUoW,
            UserManager<ApplicationUser> userManager, // حقن UserManager
            IWebHostEnvironment host) : base(host)
        {
            _context = context;
            _traineeUoW = traineeUoW;
            _organizationUoW = organizationUoW;
            _departmentUoW = departmentUoW;
            _specializationUoW = specializationUoW;
            _qualificationUoW = qualificationUoW;
            _courseTraineeUoW = courseTraineeUoW;
            _userManager = userManager;
        }

        // GET: Trainee
        public async Task<IActionResult> Index(string searchTerm = null, Guid? organizationId = null, Guid? specializationId = null)
        {
            ViewData["SearchTerm"] = searchTerm;
            ViewData["OrganizationIdFilter"] = organizationId;
            ViewData["SpecializationIdFilter"] = specializationId;


            IQueryable<Trainee> query = _traineeUoW.Entity
                                        .Include(t => t.Organizition)
                                        .Include(t => t.Department)
                                        .Include(t => t.Specialization)
                                        .Include(t => t.Qualification)
                                        .Include(t => t.ApplicationUser) // إذا أردت عرض اسم المستخدم
                                        .AsNoTracking();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.ArName.Contains(searchTerm) ||
                                         (t.EnName != null && t.EnName.Contains(searchTerm)) ||
                                         t.Email.Contains(searchTerm) ||
                                         t.PhoneNo.Contains(searchTerm) ||
                                         (t.NationalNo != null && t.NationalNo.Contains(searchTerm)));
            }
            if (organizationId.HasValue)
            {
                query = query.Where(t => t.OrganizationId == organizationId.Value);
            }
            if (specializationId.HasValue)
            {
                query = query.Where(t => t.SpecializationId == specializationId.Value);
            }


            var trainees = await query.OrderBy(t => t.ArName).ToListAsync();

            // تحويل إلى ViewModel إذا كنت ستعرض خصائص معقدة أو تحتاج لمعالجة
            // هنا، سأمرر الـ Entities مباشرة للتبسيط، ولكن يفضل استخدام ViewModel إذا كان العرض معقدًا
            // أو إذا كنت لا تريد كشف كل خصائص الـ Entity للـ View.
            // مثال التحويل (إذا احتجت):
            // var traineeVMs = trainees.Select(t => new TraineeVM { /* ... تعيين الخصائص ... */ }).ToList();

            // لتعبئة قوائم الفلاتر
            ViewBag.OrganizationsFilterList = new SelectList(await _organizationUoW.Entity.GetAll().OrderBy(o => o.Name).ToListAsync(), "Id", "Name", organizationId);
            ViewBag.SpecializationsFilterList = new SelectList(await _specializationUoW.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", specializationId);


            return View(trainees); // أو traineeVMs
        }

        // GET: Trainee/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var trainee = await _context.Trainees
                .Include(t => t.Qualification)
                .Include(t => t.Specialization)
                .Include(t => t.Organizition)
                .Include(t => t.Department)
                .Include(t => t.ApplicationUser) // إذا كنت تريد عرض معلومات الحساب
                .Include(t => t.CourseTrainees)
                    .ThenInclude(ct => ct.CourseDetails)
                        .ThenInclude(cd => cd.Course) // الكورس الرئيسي
                .Include(t => t.CourseTrainees)
                    .ThenInclude(ct => ct.CourseDetails)
                        .ThenInclude(cd => cd.Status) // حالة تفصيل الدورة
                .Include(t => t.CourseTrainees)
                    .ThenInclude(ct => ct.CourseDetails)
                        .ThenInclude(cd => cd.Location) // موقع تفصيل الدورة
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainee == null) return NotFound();
            return View(trainee);
        }

        private async Task PopulateDropdownsForViewModel(TraineeVM viewModel, bool isCreate = false)
        {
            viewModel.OrganizationsList = new SelectList(await _organizationUoW.Entity.GetAll().OrderBy(o => o.Name).ToListAsync(), "Id", "Name", viewModel.OrganizationId);
            // قائمة الأقسام يمكن أن تعتمد على المؤسسة المختارة (يتطلب AJAX أو منطق إضافي)
            // كبداية، نعرض كل الأقسام أو الأقسام المتعلقة بالمؤسسة المختارة (إذا كانت معروفة)
            if (viewModel.OrganizationId.HasValue)
            {
                viewModel.DepartmentsList = new SelectList(await _departmentUoW.Entity.GetWhere(d => d.Id == viewModel.OrganizationId).OrderBy(d => d.Name).ToListAsync(), "Id", "Name", viewModel.DepartmentId);
            }
            else
            {
                viewModel.DepartmentsList = new SelectList(await _departmentUoW.Entity.GetAll().OrderBy(d => d.Name).ToListAsync(), "Id", "Name", viewModel.DepartmentId);
            }
            viewModel.SpecializationsList = new SelectList(await _specializationUoW.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", viewModel.SpecializationId);
            viewModel.QualificationsList = new SelectList(await _qualificationUoW.Entity.GetAll().OrderBy(q => q.Name).ToListAsync(), "Id", "Name", viewModel.QualificationId);

            // قائمة المستخدمين غير المرتبطين بمتدربين بعد (أو كل المستخدمين إذا سمحت بتغيير الربط)
            var existingTraineeUserIds = await _traineeUoW.Entity.GetAll().Select(t => t.UserId).Where(uid => uid != null).ToListAsync();
            var availableUsers = await _userManager.Users
                                        .Where(u => isCreate ? !existingTraineeUserIds.Contains(u.Id) : (u.Id == viewModel.UserId || !existingTraineeUserIds.Contains(u.Id)))
                                        .OrderBy(u => u.UserName)
                                        .Select(u => new { u.Id, DisplayName = u.UserName + (string.IsNullOrEmpty(u.Email) ? "" : $" ({u.Email})") })
                                        .ToListAsync();
            viewModel.UsersList = new SelectList(availableUsers, "Id", "DisplayName", viewModel.UserId);
        }


        // GET: Trainee/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new TraineeVM();
            await PopulateDropdownsForViewModel(viewModel, isCreate: true);
            return View(viewModel);
        }

        // POST: Trainee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TraineeVM viewModel, string? isImg1) // isImg1 من الـ View لمعالجة الصورة
        {
        
            if (_traineeUoW.Entity.GetWhere(t => t.Email == viewModel.Email).Any())
                ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم مسبقًا.");
            if ( _traineeUoW.Entity.GetWhere(t => t.Email == viewModel.Email).Any())
                ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم مسبقًا.");
            if ( _traineeUoW.Entity.GetWhere(t => t.PhoneNo == viewModel.PhoneNo).Any())
                ModelState.AddModelError("PhoneNo", "رقم الهاتف مستخدم مسبقًا.");
            if (_traineeUoW.Entity.GetWhere(t => t.NationalNo == viewModel.NationalNo).Any())
                ModelState.AddModelError("NationalNo", "الرقم الوطني مستخدم مسبقًا.");


            if (ModelState.IsValid)
            {
                var trainee = new Trainee
                {
                    Id = Guid.NewGuid(),
                    ArName = viewModel.ArName,
                    EnName = viewModel.EnName,
                    PhoneNo = viewModel.PhoneNo,
                    Email = viewModel.Email,
                    NationalNo = viewModel.NationalNo,
                    Address = viewModel.Address,
                    OrganizationId = viewModel.OrganizationId,
                    DepartmentId = viewModel.DepartmentId,
                    SpecializationId = viewModel.SpecializationId,
                    QualificationId = viewModel.QualificationId,
                    UserId = viewModel.UserId, // ربط بحساب المستخدم
                    Created = DateTime.UtcNow
                };

                if (viewModel.Imge != null)
                {
                    // افترض أن UploadFile من BaseController موجودة
                    string? fileName = UploadFile("traineesprofiles", viewModel.Imge, null, isImg1); // isImg1 لتحديد إذا كانت صورة جديدة أو لا
                    trainee.ProfileImageUrl = fileName;
                }

                _traineeUoW.Entity.Insert(trainee);
                await _traineeUoW.SaveAsync();

                // لا يوجد SelectedCourseIds في TraineeVM حاليًا، إذا أضفتها، يمكنك معالجتها هنا.

                TempData["SuccessMessage"] = "تمت إضافة المتدرب بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsForViewModel(viewModel, isCreate: true); // إعادة تعبئة القوائم عند فشل التحقق
            return View(viewModel);
        }


        // GET: Trainee/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var trainee = await _traineeUoW.Entity.GetByIdAsync(id.Value);
            if (trainee == null) return NotFound();

            var viewModel = new TraineeVM
            {
                Id = trainee.Id,
                ArName = trainee.ArName,
                EnName = trainee.EnName,
                PhoneNo = trainee.PhoneNo,
                Email = trainee.Email,
                NationalNo = trainee.NationalNo,
                Address = trainee.Address,
                OrganizationId = trainee.OrganizationId,
                DepartmentId = trainee.DepartmentId,
                SpecializationId = trainee.SpecializationId,
                QualificationId = trainee.QualificationId,
                ProfileImageUrl = trainee.ProfileImageUrl, // لعرض الصورة الحالية
                UserId = trainee.UserId,
                CreatedAt = trainee.Created
            };

            await PopulateDropdownsForViewModel(viewModel);
            return View(viewModel);
        }

        // POST: Trainee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TraineeVM viewModel, string? isImg1) // isImg1 من الـ View
        {
            if (id != viewModel.Id) return NotFound();

            // التحقق من البريد الإلكتروني ورقم الهاتف المكررين (مع استثناء السجل الحالي)
            if ( _traineeUoW.Entity.GetWhere(t => t.Email == viewModel.Email && t.Id != viewModel.Id).Any())
                ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم مسبقًا لمتدرب آخر.");
            if ( _traineeUoW.Entity.GetWhere(t => t.PhoneNo == viewModel.PhoneNo && t.Id != viewModel.Id).Any())
                ModelState.AddModelError("PhoneNo", "رقم الهاتف مستخدم مسبقًا لمتدرب آخر.");
            if (viewModel.NationalNo != null &&  _traineeUoW.Entity.GetWhere(t => t.NationalNo == viewModel.NationalNo && t.Id != viewModel.Id).Any())
                ModelState.AddModelError("NationalNo", "الرقم الوطني مستخدم مسبقًا لمتدرب آخر.");


            if (ModelState.IsValid)
            {
                try
                {
                    var traineeToUpdate = await _traineeUoW.Entity.GetByIdAsync(viewModel.Id);
                    if (traineeToUpdate == null) return NotFound();

                    traineeToUpdate.ArName = viewModel.ArName;
                    traineeToUpdate.EnName = viewModel.EnName;
                    traineeToUpdate.PhoneNo = viewModel.PhoneNo;
                    traineeToUpdate.Email = viewModel.Email;
                    traineeToUpdate.NationalNo = viewModel.NationalNo;
                    traineeToUpdate.Address = viewModel.Address;
                    traineeToUpdate.OrganizationId = viewModel.OrganizationId;
                    traineeToUpdate.DepartmentId = viewModel.DepartmentId;
                    traineeToUpdate.SpecializationId = viewModel.SpecializationId;
                    traineeToUpdate.QualificationId = viewModel.QualificationId;
                    traineeToUpdate.UserId = viewModel.UserId;
                    traineeToUpdate.Modified = DateTime.UtcNow;

                    if (viewModel.Imge != null)
                    {
                        // UploadFile يجب أن تتعامل مع حذف الصورة القديمة إذا تم تمريرها
                        string? newFileName = UploadFile("trainees_profiles", viewModel.Imge, traineeToUpdate.ProfileImageUrl, isImg1);
                        traineeToUpdate.ProfileImageUrl = newFileName;
                    }
                    else if (isImg1 == "false" && !string.IsNullOrEmpty(traineeToUpdate.ProfileImageUrl)) // إذا تم حذف الصورة من الواجهة
                    {
                        // افترض أن DeleteFile من BaseController موجودة
                        DeleteOldFile(Path.Combine("trainees_profiles", traineeToUpdate.ProfileImageUrl)); // أو الطريقة التي تتعامل بها مع المسارات
                        traineeToUpdate.ProfileImageUrl = null;
                    }
                    // إذا لم يتم رفع ملف ولم يتم حذف الصورة (isImg1 != "false")، تبقى الصورة القديمة

                    _traineeUoW.Entity.Update(traineeToUpdate);
                    await _traineeUoW.SaveAsync();
                    TempData["SuccessMessage"] = "تم تعديل بيانات المتدرب بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TraineeExists(viewModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsForViewModel(viewModel);
            return View(viewModel);
        }

        // GET: Trainee/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var trainee = await _traineeUoW.Entity
                .Include(t => t.Organizition) // لعرض اسم المؤسسة في صفحة التأكيد
                .Include(t => t.Specialization) // لعرض اسم التخصص
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainee == null) return NotFound();

            // تحويل إلى ViewModel إذا كان View الحذف يتوقع ViewModel
            var viewModel = new TraineeVM
            {
                Id = trainee.Id,
                ArName = trainee.ArName,
                Email = trainee.Email,
                // ... أي خصائص أخرى تريد عرضها في صفحة تأكيد الحذف
            };
            return View(viewModel); // أو return View(trainee) إذا كان View الحذف يتوقع Entity
        }


        // POST: Trainee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var trainee = await _traineeUoW.Entity.GetByIdAsync(id);
            if (trainee == null)
            {
                TempData["ErrorMessage"] = "المتدرب غير موجود.";
                return RedirectToAction(nameof(Index));
            }

            // 1. حذف التسجيلات المرتبطة في CourseTrainee (إذا كان OnDelete ليس Cascade)
            var courseRegistrations = await _courseTraineeUoW.Entity.GetWhere(ct => ct.TraineeId == id).ToListAsync();
            foreach (var registration in courseRegistrations)
            {
                _courseTraineeUoW.Entity.Delete(registration);
            }
            await _courseTraineeUoW.SaveAsync(); // حفظ التغييرات على CourseTrainee

            // 2. حذف الصورة الشخصية (إذا موجودة)
            if (!string.IsNullOrEmpty(trainee.ProfileImageUrl))
            {
                // افترض أن DeleteOldFile من BaseController موجودة وتتعامل مع المسارات بشكل صحيح
                DeleteOldFile(Path.Combine("trainees_profiles", trainee.ProfileImageUrl));
            }

            // 3. حذف المتدرب نفسه
            _traineeUoW.Entity.Delete(trainee);
            await _traineeUoW.SaveAsync();

            TempData["SuccessMessage"] = "تم حذف المتدرب بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> TraineeExists(Guid id)
        {
            return await _traineeUoW.Entity.GetByIdAsync(id) != null;
        }


        // دالة مساعدة لـ AJAX لجلب الأقسام بناءً على المؤسسة
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByOrganization(Guid organizationId)
        {
            var departments = await _departmentUoW.Entity
                .GetWhere(d => d.Trainees.Any(t => t.OrganizationId == organizationId))
                .OrderBy(d => d.Name)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToListAsync();
            return Json(departments);
        }


        [HttpGet]
        public async Task<IActionResult> SearchTrainees(string term, int page = 1, int pageSize = 20, Guid? courseDetailsIdToExclude = null)
        {
            // 'term' هو مصطلح البحث الذي يكتبه المستخدم
            // 'page' لدعم ترقيم الصفحات في نتائج البحث (إذا كانت النتائج كثيرة)
            // 'pageSize' عدد النتائج لكل صفحة
            // 'courseDetailsIdToExclude' (اختياري) لاستبعاد المتدربين المسجلين بالفعل في دورة معينة

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2) // يمكنك تعديل الحد الأدنى لعدد الأحرف
            {
                // لا ترجع شيئاً أو أرجع قائمة فارغة إذا كان البحث قصيراً جداً
                return Json(new { results = new List<object>(), total_count = 0 });
            }

            var query = _context.Trainees.AsQueryable();

            // تطبيق فلتر البحث (عدل هذه لتناسب الحقول التي تريد البحث فيها في Trainee Entity)
            // افترض أن Trainee Entity لديه Name, Email, وربما NationalId أو TraineeCode
            string searchTermLower = term.ToLower();
            query = query.Where(t =>
                (t.ArName != null && t.ArName.ToLower().Contains(searchTermLower)) ||
                (t.Email != null && t.Email.ToLower().Contains(searchTermLower)) ||
                // (t.NationalId != null && t.NationalId.Contains(term)) || // مثال إذا كان لديك NationalId
                (t.PhoneNo != null && t.PhoneNo.Contains(term)) // مثال إذا كان لديك PhoneNumber
                                                                        // أضف أي حقول أخرى تريد البحث فيها
            );

            // استبعاد المتدربين المسجلين بالفعل في الدورة المحددة (إذا تم تمرير courseDetailsIdToExclude)
            if (courseDetailsIdToExclude.HasValue)
            {
                var enrolledTraineeIdsInSpecificCourse = await _context.CourseTrainees
                    .Where(ct => ct.CourseDetailsId == courseDetailsIdToExclude.Value)
                    .Select(ct => ct.TraineeId)
                    .Distinct() // تأكد من عدم وجود تكرار إذا كان هناك خطأ ما في البيانات
                    .ToListAsync();

                if (enrolledTraineeIdsInSpecificCourse.Any())
                {
                    query = query.Where(t => !enrolledTraineeIdsInSpecificCourse.Contains(t.Id));
                }
            }

            // حساب العدد الإجمالي قبل الترقيم (مهم لـ Select2 pagination)
            var totalCount = await query.CountAsync();

            // تطبيق الترقيم
            var trainees = await query
                .OrderBy(t => t.ArName) // أو أي ترتيب تفضله
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new // التنسيق الذي يتوقعه Select2 (أو أي مكتبة أخرى)
                {
                    id = t.Id, // قيمة الـ option
                    text = $"{t.ArName}{(t.Email != null ? $" ({t.Email})" : "")}{(t.PhoneNo != null ? $" - {t.PhoneNo}" : "")}" // النص الذي سيظهر للمستخدم
                    // يمكنك إضافة أي بيانات إضافية هنا إذا احتجتها في templateResult
                    // مثلاً: description = t.SomeOtherField
                })
                .ToListAsync();

            // Select2 يتوقع JSON يحتوي على خاصية 'results'
            // وإذا كنت تدعم ترقيم الصفحات من جانب الخادم، يتوقع 'pagination: { more: true/false }'
            return Json(new
            {
                results = trainees,
                total_count = totalCount, // إرسال العدد الإجمالي للنتائج
                pagination = new { more = (page * pageSize) < totalCount }
            });
        }
    }
}