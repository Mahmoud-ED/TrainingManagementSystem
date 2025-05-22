using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    public class SpecializationController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _host;
        private readonly IEmailSender _emailSender;
        private readonly UserSessionTracker _userSessionTracker;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork<Employee> _employee;
        private readonly IUnitOfWork<TrainerSpecialization> _trainerSpecialization;
        private readonly IUnitOfWork<UserProfile> _userProfile;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWork<Specialization> _specialization;

        public SpecializationController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IWebHostEnvironment host,
                                 IEmailSender emailSender,
                                 UserSessionTracker userSessionTracker,
                                 RoleManager<IdentityRole> roleManager,
                                 IUnitOfWork<Employee> employee,
                                 IUnitOfWork<TrainerSpecialization> trainerSpecialization,
                                 IUnitOfWork<UserProfile> userProfile,
                                 IUnitOfWork<Specialization> specialization,
                                 IServiceProvider serviceProvider) : base(host)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _host = host;
            _emailSender = emailSender;
            _userSessionTracker = userSessionTracker;
            _roleManager = roleManager;
            _employee = employee;
            _trainerSpecialization = trainerSpecialization;
            _userProfile = userProfile;
            _serviceProvider = serviceProvider;
            _specialization = specialization;
        }

        // GET: Specializations
        public async Task<IActionResult> Index()
        {
            // جلب جميع التخصصات من قاعدة البيانات
            var specializations = await _specialization.Entity.GetAll().ToListAsync();
            return View(specializations); // إرسال قائمة التخصصات إلى الواجهة
        }


        public async Task<IActionResult> Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,IsActive")] Specialization specialization)
        {
            if (ModelState.IsValid)
            {
                if (_specialization.Entity.GetWhere(s => s.Name == specialization.Name).Any())
                {
                    ModelState.AddModelError("Name", "اسم التخصص موجود مسبقًا.");
                    return View(specialization);
                }

                specialization.Id = Guid.NewGuid();
                specialization.Created = DateTime.UtcNow;
                specialization.Modified = DateTime.UtcNow;

                // Using IUnitOfWork to insert the specialization
                _specialization.Entity.Insert(specialization);
                await _specialization.SaveAsync();

                TempData["SuccessMessage"] = "تم إضافة التخصص بنجاح!";
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف التخصص غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index));
            }

            var specialization = await _specialization.Entity.GetByIdAsync(id.Value);

            if (specialization == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على التخصص المطلوب تعديله.";
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        // POST: Specializations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name")] Specialization specialization)
        {
            if (id != specialization.Id)
            {
                TempData["ErrorMessage"] = "خطأ في تطابق معرف التخصص.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing =  _specialization.Entity.GetWhere(s => s.Name == specialization.Name && s.Id != specialization.Id).FirstOrDefault();
                     if (existing != null)
                    {
                        ModelState.AddModelError("Name", "اسم التخصص موجود مسبقًا لتخصص آخر.");
                        return View(specialization);
                    }

                    specialization.Modified = DateTime.UtcNow;

                   
                    _specialization.Entity.Update(specialization);
                    await _specialization.SaveAsync();

                    TempData["SuccessMessage"] = "تم تعديل التخصص بنجاح!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await SpecializationExists(specialization.Id)) 
                    {
                        TempData["ErrorMessage"] = "لم يتم العثور على التخصص أثناء محاولة التحديث.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "حدث خطأ أثناء محاولة حفظ التغييرات. قد يكون السجل قد تم تعديله بواسطة مستخدم آخر.");
                        return View(specialization);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        private async Task<bool> SpecializationExists(Guid id)
        {
            return await _specialization.Entity.GetByIdAsync(id) != null;
        }

        // GET: Specializations/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف التخصص غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index));
            }
            var specialization = await _specialization.Entity.GetByIdAsync(id.Value);
            if (specialization == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على التخصص المطلوب حذفه.";
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        // POST: Specializations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var specialization = await _specialization.Entity.GetByIdAsync(id);
            if (specialization == null)
            {
                TempData["ErrorMessage"] = "التخصص غير موجود أو قد تم حذفه بالفعل.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
              
                bool isUsedByTrainers = await _trainerSpecialization.Entity.GetWhere(ts => ts.SpecializationId == id).AnyAsync();

                if (isUsedByTrainers)
                {
                    TempData["ErrorMessage"] = $"لا يمكن حذف التخصص '{specialization.Name}' لأنه مستخدم من قبل مدرب واحد على الأقل. يرجى تحديث بيانات المدربين أولاً.";
                    return RedirectToAction(nameof(Index));
                }

                _specialization.Entity.Delete(specialization);
                await _specialization.SaveAsync();
                TempData["SuccessMessage"] = $"تم حذف التخصص '{specialization.Name}' بنجاح.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = $"لا يمكن حذف التخصص '{specialization.Name}' لأنه مرتبط بسجلات أخرى. يرجى إزالة الارتباطات أولاً.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "حدث خطأ غير متوقع أثناء محاولة حذف التخصص.";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

       
    }
    }
