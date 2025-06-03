using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.EntityFrameworkCore; // For EF Core operations like Include, ThenInclude
using System;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models; // افترض أن هذا هو الـ Namespace للـ DbContext
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels; // افترض أنك ستنشئ مجلد ViewModels
namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]

    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context; // استبدل ApplicationDbContext باسم الـ DbContext الخاص بك

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index(
            string searchString,
            Guid? classificationId,
            Guid? levelId,
            Guid? parentId,
            int? pageNumber)
        {
            // ViewData للتجميل فقط، الأفضل استخدام ViewModel
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentClassification"] = classificationId;
            ViewData["CurrentLevel"] = levelId;
            ViewData["CurrentParent"] = parentId;

            var coursesQuery = _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .Include(c => c.CourseDetails) // لحساب عدد CourseDetails
                .Include(c => c.CourseTrainers) // لحساب عدد المدربين
                .AsQueryable(); // للبدء ببناء الاستعلام

            if (!String.IsNullOrEmpty(searchString))
            {
                coursesQuery = coursesQuery.Where(c => c.Name.Contains(searchString) || c.Code.Contains(searchString));
            }

            if (classificationId.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.CourseClassificationId == classificationId.Value);
            }

            if (levelId.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.LevelId == levelId.Value);
            }

            if (parentId.HasValue)
            {
                // إذا كان parentId له قيمة، فلتر بناءً عليه
                // إذا لم يكن له قيمة، ولكن المستخدم اختار "بدون دورة أم"، ستحتاج لمعالجة خاصة
                // حالياً، إذا كان null، لا يتم الفلترة به
                coursesQuery = coursesQuery.Where(c => c.CourseParentId == parentId.Value);
            }

            // هنا ستستخدم الـ ViewModel الذي اقترحناه (CourseIndexViewModel)
            // وسنقوم بتعبئة الـ SelectLists للـ dropdowns في الـ View
            var courseItems = await coursesQuery
                .Select(c => new CourseItemViewModel // افترض أن CourseItemViewModel موجود في ViewModels
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    CourseClassificationName = c.CourseClassification != null ? c.CourseClassification.Name : "N/A",
                    LevelName = c.Level != null ? c.Level.Name : "N/A",
                    CourseParentName = c.CourseParent != null ? c.CourseParent.Name : null,
                    CourseDetailsCount = c.CourseDetails != null ? c.CourseDetails.Count : 0,
                    TrainersCount = c.CourseTrainers != null ? c.CourseTrainers.Count : 0
                })
                .ToListAsync(); // أو استخدم مكتبة ترقيم الصفحات مثل X.PagedList

            // إعداد الـ ViewModel للـ Index
            var viewModel = new CourseIndexViewModel
            {
                Courses = courseItems, // هنا يجب أن تكون النتيجة بعد الترقيم
                SearchString = searchString,
                SelectedCourseClassificationId = classificationId,
                SelectedLevelId = levelId,
                SelectedCourseParentId = parentId,
                CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(cc => cc.Name).ToListAsync(), "Id", "Name", classificationId),
                Levels = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", levelId),
                CourseParents = new SelectList(await _context.CourseParent.OrderBy(cp => cp.Name).ToListAsync(), "Id", "Name", parentId)
                // تحتاج لإضافة Pagination هنا
            };

            int pageSize = 10; // عدد العناصر في كل صفحة
            // var pagedList = await PaginatedList<CourseItemViewModel>.CreateAsync(courseItems.AsQueryable(), pageNumber ?? 1, pageSize);
            // viewModel.Courses = pagedList; // إذا استخدمت PaginatedList
            // viewModel.PageIndex = pagedList.PageIndex;
            // viewModel.TotalPages = pagedList.TotalPages;


            return View(viewModel); // مرر الـ ViewModel الكامل
        }

   
        // GET: Courses/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CourseFormViewModel
            {
                CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                Levels = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name"),
                CourseParents = new SelectList(await _context.CourseParent.OrderBy(cp => cp.Name).ToListAsync(), "Id", "Name"),
                AvailableTrainers = await _context.Trainers
                                        .OrderBy(t => t.ArName)
                                        .Select(t => new TrainerCheckboxViewModel { Id = t.Id, Name = t.ArName, IsSelected = false })
                                        .ToListAsync(),
                // الجديد: ملء القوائم المنسدلة لـ CourseDetails
                Locations = new SelectList(await _context.Locations.OrderBy(l => l.Name).ToListAsync(), "Id", "Name"),
                CourseTypes = new SelectList(await _context.CourseTypes.OrderBy(ct => ct.Name).ToListAsync(), "Id", "Name"),
                Statuses = new SelectList(await _context.Statuses.OrderBy(s => s.Name).ToListAsync(), "Id", "Name"),

                // يمكنك إضافة عنصر تفاصيل فارغ واحد ليبدأ به المستخدم
                // CourseDetailsEntries = new List<CourseDetailFormEntryViewModel> { new CourseDetailFormEntryViewModel() }
            };
            // إذا لم تضف عنصراً افتراضياً، سيبدأ المستخدم بدون أي فورم CourseDetails ظاهر
            // وسيعتمد على JavaScript لإضافة أول فورم.

            return View(viewModel);
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseFormViewModel viewModel)
        {
            // التحقق من صحة المدخلات لـ CourseDetailsEntries يدوياً إذا لم يكن [Required] كافياً
            // أو الاعتماد على DataAnnotations في CourseDetailFormEntryViewModel
            if (viewModel.CourseDetailsEntries == null || !viewModel.CourseDetailsEntries.Any())
            {
                // يمكنك إضافة خطأ إذا كان يجب إضافة تفصيل واحد على الأقل
                ModelState.AddModelError("CourseDetailsEntries", "يجب إضافة تفصيل واحد على الأقل للدورة.");
            }
            // تأكد من التحقق من صحة كل عنصر في CourseDetailsEntries
            // TryValidateModel(viewModel.CourseDetailsEntries) قد لا يعمل مباشرة مع القوائم المعقدة
            // قد تحتاج للمرور عليها والتحقق من كل عنصر إذا لزم الأمر

            //if (ModelState.IsValid)
            //{
            var course = new Course
            {
                Created = DateTime.Now,
                Name = viewModel.Name,
                Code = viewModel.Code,
                Description = viewModel.Description,
                CourseClassificationId = viewModel.CourseClassificationId,
                LevelId = viewModel.LevelId,
                CourseParentId = viewModel.CourseParentId,
                // BaseEntity properties
            };
                _context.Add(course);
                // لا تحفظ هنا بعد

                // إضافة المدربين المختارين (CourseTrainer)
                if (viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Any())
                {
                    foreach (var trainerId in viewModel.SelectedTrainerIds)
                    {
                        var courseTrainer = new CourseTrainer
                        {
                            Created =DateTime.Now, // سيتم تعيين الـ ID بعد الإضافة الأولى للـ Course
                            CourseId = course.Id, // سيتم تعيين الـ ID بعد الإضافة الأولى للـ Course
                            TrainerId = trainerId
                        };
                        _context.CourseTrainers.Add(courseTrainer);
                    }
                }

                // الجديد: إضافة تفاصيل الدورة (CourseDetails)
                if (viewModel.CourseDetailsEntries != null)
                {
                    foreach (var entryVm in viewModel.CourseDetailsEntries)
                    {
                        // تحقق إضافي لكل عنصر إذا لزم الأمر
                        if (entryVm.DurationHours > 0) // مثال على تحقق بسيط
                        {
                            var courseDetail = new CourseDetails
                            {
                              Created = DateTime.Now, // تعيين تاريخ الإنشاء    ss
                                CourseId = course.Id, // ربطها بالدورة الرئيسية
                                DurationHours = entryVm.DurationHours,
                                StartDate = entryVm.StartDate,
                                EndDate = entryVm.EndDate,
                                LocationId = entryVm.LocationId,
                                CourseTypeId = entryVm.CourseTypeId,
                                StatusId = entryVm.StatusId
                                // BaseEntity properties
                            };
                            _context.CourseDetails.Add(courseDetail);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم إنشاء الدورة وتفاصيلها بنجاح!";
                return RedirectToAction(nameof(Index));
            //}

            //// إذا فشل الـ ModelState, أعد ملء الـ SelectLists
            //await PopulateFormViewModelDropdowns(viewModel); // دالة مساعدة جديدة
            return View(viewModel);
        }

   
        [HttpGet]
        public async Task<IActionResult> GetCourseDetailEntryPartial(int index)
        {
            // نحتاج إلى تمرير القوائم المنسدلة إلى الـ Partial View
            // حتى لو كان الـ Model الرئيسي (CourseDetailFormEntryViewModel) فارغاً
            var parentViewModelForDropdowns = new CourseFormViewModel
            {
                Locations = new SelectList(await _context.Locations.OrderBy(l => l.Name).ToListAsync(), "Id", "Name"),
                CourseTypes = new SelectList(await _context.CourseTypes.OrderBy(ct => ct.Name).ToListAsync(), "Id", "Name"),
                Statuses = new SelectList(await _context.Statuses.OrderBy(s => s.Name).ToListAsync(), "Id", "Name")
                // لا نحتاج لملء بقية خصائص CourseFormViewModel هنا، فقط القوائم المنسدلة
            };

            ViewData["index"] = index;
            ViewData["ParentViewModel"] = parentViewModelForDropdowns;
            // ParentListSize مهم لإخفاء زر الحذف في أول عنصر إذا كان هو الوحيد.
            // عند الإضافة عبر AJAX، يمكننا افتراض أننا نضيف إلى قائمة قد تحتوي على عناصر أخرى
            // أو أن JavaScript سيتولى إظهار/إخفاء زر الحذف.
            // للتبسيط، يمكن تمرير قائمة فارغة هنا، لأن المنطق الرئيسي لـ isOnlyOneEntry في الـ Partial
            // يعتمد أكثر على عدد العناصر الموجودة في الـ DOM عند الإضافة.
            ViewData["ParentListSize"] = new List<CourseDetailFormEntryViewModel>(); // أو يمكنك تمرير قيمة تعكس العدد الحالي من الـ client إذا أردت

            // نرجع الـ Partial View مع Model جديد فارغ (أو مهيأ بقيم افتراضية إذا أردت)
            return PartialView("_CourseDetailFormEntry", new CourseDetailFormEntryViewModel { StartDate = DateTime.Today });
        }

        // ... (بقية الـ Actions في CoursesController)



        private async Task PopulateFormViewModelDropdowns(CourseFormViewModel viewModel)
        {
            viewModel.CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.CourseClassificationId);
            viewModel.Levels = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", viewModel.LevelId);
            viewModel.CourseParents = new SelectList(await _context.CourseParent.OrderBy(cp => cp.Name).ToListAsync(), "Id", "Name", viewModel.CourseParentId);

            var allTrainers = await _context.Trainers.OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainers = allTrainers.Select(t => new TrainerCheckboxViewModel
            {
                Id = t.Id,
                Name = t.ArName,
                IsSelected = viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Contains(t.Id)
            }).ToList();

            viewModel.Locations = new SelectList(await _context.Locations.OrderBy(l => l.Name).ToListAsync(), "Id", "Name");
            viewModel.CourseTypes = new SelectList(await _context.CourseTypes.OrderBy(ct => ct.Name).ToListAsync(), "Id", "Name");
            viewModel.Statuses = new SelectList(await _context.Statuses.OrderBy(s => s.Name).ToListAsync(), "Id", "Name");

            // إذا كنت تريد إعادة ملء قيم CourseDetailsEntries عند الفشل، فهذا أكثر تعقيداً
            // لأنك تحتاج إلى الحفاظ على القيم التي أدخلها المستخدم.
            // الـ Model Binding سيفعل ذلك تلقائياً إذا كانت أسماء الحقول صحيحة.
        }
        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                                .Include(c => c.CourseTrainers) // لجلب المدربين المعينين
                                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            var viewModel = new CourseFormViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Code,
                Description = course.Description,
                CourseClassificationId = course.CourseClassificationId,
                LevelId = course.LevelId,
                CourseParentId = course.CourseParentId,
                CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", course.CourseClassificationId),
                Levels = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", course.LevelId),
                CourseParents = new SelectList(await _context.CourseParent.OrderBy(cp => cp.Name).ToListAsync(), "Id", "Name", course.CourseParentId),
                SelectedTrainerIds = course.CourseTrainers.Select(ct => ct.TrainerId).ToList() // المدربون المختارون حالياً
            };

            // بناء قائمة المدربين مع تحديد المختارين
            var allTrainers = await _context.Trainers.OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainers = allTrainers.Select(t => new TrainerCheckboxViewModel
            {
                Id = t.Id,
                Name = t.ArName, // افترض أن Trainer لديه Name
                IsSelected = viewModel.SelectedTrainerIds.Contains(t.Id)
            }).ToList();

            return View(viewModel);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CourseFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var courseToUpdate = await _context.Courses
                                                .Include(c => c.CourseTrainers) // مهم جداً لتحديث المدربين
                                                .FirstOrDefaultAsync(c => c.Id == id);

                    if (courseToUpdate == null)
                    {
                        return NotFound();
                    }

                    // 1. تحديث بيانات Course الأساسية
                    courseToUpdate.Name = viewModel.Name;
                    courseToUpdate.Code = viewModel.Code;
                    courseToUpdate.Description = viewModel.Description;
                    courseToUpdate.CourseClassificationId = viewModel.CourseClassificationId;
                    courseToUpdate.LevelId = viewModel.LevelId;
                    courseToUpdate.CourseParentId = viewModel.CourseParentId;
                    // courseToUpdate.UpdatedDate = DateTime.UtcNow; // إذا كان لديك

                    _context.Update(courseToUpdate); // وضع علامة التعديل على الكيان الرئيسي

                    // 2. تحديث المدربين (CourseTrainer)
                    // المدربون المختارون حالياً من الفورم
                    var selectedTrainerIdsFromForm = viewModel.SelectedTrainerIds ?? new List<Guid>();

                    // المدربون المعينون حالياً للدورة في قاعدة البيانات
                    var currentTrainerIdsInDb = courseToUpdate.CourseTrainers.Select(ct => ct.TrainerId).ToList();

                    // المدربون المطلوب إضافتهم: موجودون في الفورم وليسوا في قاعدة البيانات
                    var trainersToAdd = selectedTrainerIdsFromForm.Except(currentTrainerIdsInDb).ToList();
                    foreach (var trainerId in trainersToAdd)
                    {
                        _context.CourseTrainers.Add(new CourseTrainer { CourseId = courseToUpdate.Id, TrainerId = trainerId });
                    }

                    // المدربون المطلوب حذفهم: موجودون في قاعدة البيانات وليسوا في الفورم
                    var trainersToRemoveIds = currentTrainerIdsInDb.Except(selectedTrainerIdsFromForm).ToList();
                    var courseTrainersToRemove = courseToUpdate.CourseTrainers
                                                    .Where(ct => trainersToRemoveIds.Contains(ct.TrainerId))
                                                    .ToList();
                    _context.CourseTrainers.RemoveRange(courseTrainersToRemove);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تعديل الدورة بنجاح!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // يمكنك إضافة معالجة خاصة هنا إذا أردت
                        ModelState.AddModelError("", "حدث خطأ أثناء محاولة حفظ التغييرات. تم تعديل البيانات بواسطة مستخدم آخر.");
                        // إعادة ملء البيانات للـ View
                        await PopulateEditViewModelDropdownsAndTrainers(viewModel);
                        return View(viewModel);
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // إذا فشل الـ ModelState, أعد ملء الـ SelectLists والـ AvailableTrainers
            await PopulateEditViewModelDropdownsAndTrainers(viewModel);
            return View(viewModel);
        }


        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }
            // يمكنك هنا عرض اسم الدورة لتأكيد الحذف
            // لا حاجة لـ ViewModel معقد هنا، يمكن استخدام الـ Entity مباشرة أو ViewModel بسيط جداً
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var course = await _context.Courses
                                .Include(c => c.CourseTrainers) // لحذف العلاقات الوسيطة
                                .Include(c => c.CourseDetails) // للنظر في كيفية التعامل مع التفاصيل
                                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // هنا يجب أن تقرر سياسة الحذف:
            // 1. هل تحذف الـ CourseDetails المرتبطة؟ (Cascade delete إذا تم إعداده في قاعدة البيانات)
            // 2. أو تمنع حذف الدورة إذا كان لديها تفاصيل (CourseDetails)؟
            // 3. أو تجعل CourseId في CourseDetails nullable وتفصلها (أقل شيوعاً هنا)

            // مثال: منع الحذف إذا كان هناك تفاصيل دورات
            if (course.CourseDetails != null && course.CourseDetails.Any())
            {
                // أرسل رسالة خطأ للمستخدم
                TempData["ErrorMessage"] = "لا يمكن حذف هذه الدورة لأنها تحتوي على تفاصيل دورات منفذة. يرجى حذف التفاصيل أولاً.";
                // أعد توجيهه إلى صفحة التفاصيل أو صفحة الحذف مرة أخرى مع الرسالة
                return RedirectToAction(nameof(Delete), new { id = id });
                // أو
                // ModelState.AddModelError("", "لا يمكن حذف هذه الدورة لأنها تحتوي على تفاصيل دورات منفذة. يرجى حذف التفاصيل أولاً.");
                // return View(course); // سيعرض صفحة الحذف مرة أخرى مع رسالة الخطأ
            }

            // حذف علاقات CourseTrainer المرتبطة
            if (course.CourseTrainers != null && course.CourseTrainers.Any())
            {
                _context.CourseTrainers.RemoveRange(course.CourseTrainers);
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم حذف الدورة بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // دالة مساعدة لإعادة ملء القوائم المنسدلة وقائمة المدربين في حالة فشل التحقق من صحة النموذج في Edit (POST)
        private async Task PopulateEditViewModelDropdownsAndTrainers(CourseFormViewModel viewModel)
        {
            viewModel.CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.CourseClassificationId);
            viewModel.Levels = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", viewModel.LevelId);
            viewModel.CourseParents = new SelectList(await _context.CourseParent.OrderBy(cp => cp.Name).ToListAsync(), "Id", "Name", viewModel.CourseParentId);

            var allTrainers = await _context.Trainers.OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainers = allTrainers.Select(t => new TrainerCheckboxViewModel
            {
                Id = t.Id,
                Name = t.ArName, // افترض أن Trainer لديه Name
                IsSelected = viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Contains(t.Id)
            }).ToList();
        }


        // (داخل CourseDetailsController.cs)
        // GET: CourseDetails/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseDetails = await _context.CourseDetails
                .Include(cd => cd.Location)
                .Include(cd => cd.CourseType)
                .Include(cd => cd.Status)
                .Include(cd => cd.CourseTrainees)
                    .ThenInclude(ct => ct.Trainee)
                .Include(cd => cd.Course) // *** جلب الكورس الرئيسي (المنهج) ***
                    .ThenInclude(c => c.CourseClassification) // تصنيف الكورس الرئيسي
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.Level) // مستوى الكورس الرئيسي
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.CourseParent) // الدورة الأم للكورس الرئيسي
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.CourseTrainers) // *** المدربون المعينون للكورس الرئيسي ***
                        .ThenInclude(coursetrainer => coursetrainer.Trainer) // لجلب أسماء المدربين
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (courseDetails == null)
            {
                return NotFound();
            }

            var viewModel = new CourseDetailsDisplayViewModel
            {
                // بيانات CourseDetails الحالية
                Id = courseDetails.Id,
                StartDate = courseDetails.StartDate,
                EndDate = courseDetails.EndDate,
                DurationHours = courseDetails.DurationHours,
                LocationName = courseDetails.Location?.Name ?? "N/A",
                CourseTypeName = courseDetails.CourseType?.Name ?? "N/A",
                StatusName = courseDetails.Status?.Name ?? "N/A",

                // بيانات Course الرئيسي
                ParentCourseId = courseDetails.CourseId,
               
                // المتدربون المسجلون في هذا الـ CourseDetails
                EnrolledTrainees = courseDetails.CourseTrainees.Select(ct => new EnrolledTraineeViewModel
                {
                    CourseTraineeId = ct.Id,
                    TraineeId = ct.TraineeId,
                    TraineeName = ct.Trainee?.ArName ?? "N/A",
                    TraineeEmail = ct.Trainee?.Email,
                    AttendancePercentage = ct.AttendancePercentage,
                    Grade = ct.Grade,
                    CertificateNumber = ct.CertificateNumber,
                    CertificateIssueDate = ct.CertificateIssueDate
                }).OrderBy(t => t.TraineeName).ToList()
            };

            // تعديل عنوان الصفحة ليشمل اسم الكورس الرئيسي إذا كان مختلفاً
            ViewData["Title"] = $"تفاصيل تنفيذ: {courseDetails.Course?.Name ?? "دورة"} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})";

            return View(viewModel);
        }
        public async Task<IActionResult> EnrollTrainee(Guid courseDetailsId)
        {
            var courseDetails = await _context.CourseDetails
                                            .Include(cd => cd.Course)
                                            .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);
            if (courseDetails == null)
            {
                return NotFound();
            }


            // جلب المتدربين الذين لم يتم تسجيلهم بعد في هذه الدورة المنفذة
            var enrolledTraineeIds = await _context.CourseTrainees
                                            .Where(ct => ct.CourseDetailsId == courseDetailsId)
                                            .Select(ct => ct.TraineeId)
                                            .ToListAsync();

            var availableTrainees = await _context.Trainees
                                            .Where(t => !enrolledTraineeIds.Contains(t.Id)) // استبعاد المسجلين
                                            .OrderBy(t => t.ArName)
                                            .ToListAsync();

            var viewModel = new EnrollTraineeViewModel
            {
                CourseId= courseDetails.CourseId,
                CourseDetailsId = courseDetailsId,
                CourseDetailsName = $"{courseDetails.Course.Name} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})",
                AvailableTrainees = new SelectList(availableTrainees, "Id", "ArName") // افترض أن Trainee لديه Name
            };

            return View("EnrollTrainee", viewModel); // ستحتاج لإنشاء View لهذا
        }

        // POST: /EnrollTrainee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollTrainee(EnrollTraineeViewModel viewModel)
        {
            //if (ModelState.IsValid)
            //{
                // تحقق مرة أخرى أن المتدرب ليس مسجلاً بالفعل (حماية إضافية)
                bool alreadyEnrolled = await _context.CourseTrainees
                    .AnyAsync(ct => ct.CourseDetailsId == viewModel.CourseDetailsId && ct.TraineeId == viewModel.SelectedTraineeId);

                if (alreadyEnrolled)
                {
                    ModelState.AddModelError("SelectedTraineeId", "هذا المتدرب مسجل بالفعل في هذه الدورة.");
                }
                else
                {
                    var courseTrainee = new CourseTrainee
                    {
                        CourseDetailsId = viewModel.CourseDetailsId,
                        TraineeId = viewModel.SelectedTraineeId,
                        Notes = viewModel.Notes
                        // يمكنك تعيين قيم افتراضية أخرى هنا إذا أردت
                    };
                    _context.Add(courseTrainee);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تسجيل المتدرب بنجاح!";
                    return RedirectToAction(nameof(Details), new { id = viewModel.CourseId });
                }
            //}

            // إذا فشل التحقق أو كان مسجلاً بالفعل، أعد ملء القوائم
            var courseDetails = await _context.CourseDetails.Include(cd => cd.Course).FirstOrDefaultAsync(cd => cd.Id == viewModel.CourseDetailsId);
            if (courseDetails != null)
            {
                viewModel.CourseDetailsName = $"{courseDetails.Course.Name} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})";
            }
            var enrolledTraineeIds = await _context.CourseTrainees.Where(ct => ct.CourseDetailsId == viewModel.CourseDetailsId).Select(ct => ct.TraineeId).ToListAsync();
            var availableTrainees = await _context.Trainees.Where(t => !enrolledTraineeIds.Contains(t.Id)).OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainees = new SelectList(availableTrainees, "Id", "Name", viewModel.SelectedTraineeId);

            return View(viewModel);
        }

        // GET: /EditEnrollment/{courseTraineeId}
        public async Task<IActionResult> EditEnrollment(Guid courseTraineeId)
        {
            var courseTrainee = await _context.CourseTrainees
                                        .Include(ct => ct.Trainee)
                                        .Include(ct => ct.CourseDetails)
                                            .ThenInclude(cd => cd.Course)
                                        .FirstOrDefaultAsync(ct => ct.Id == courseTraineeId);

            if (courseTrainee == null)
            {
                return NotFound();
            }

            var viewModel = new EditEnrollmentViewModel
            {
                CourseTraineeId = courseTrainee.Id,
                CourseDetailsId = courseTrainee.CourseDetailsId,
                CourseName = courseTrainee.CourseDetails.Course?.Name ?? "N/A",
                TraineeName = courseTrainee.Trainee?.ArName ?? "N/A",
                AttendancePercentage = courseTrainee.AttendancePercentage,
                Grade = courseTrainee.Grade,
                CertificateNumber = courseTrainee.CertificateNumber,
                CertificateUrl = courseTrainee.CertificateUrl,
                CertificateIssueDate = courseTrainee.CertificateIssueDate,
                Notes = courseTrainee.Notes
            };
            return View(viewModel); // ستحتاج لإنشاء View لهذا
        }

        // POST: /EditEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(EditEnrollmentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var courseTraineeToUpdate = await _context.CourseTrainees.FindAsync(viewModel.CourseTraineeId);
                if (courseTraineeToUpdate == null)
                {
                    return NotFound();
                }

                courseTraineeToUpdate.AttendancePercentage = viewModel.AttendancePercentage;
                courseTraineeToUpdate.Grade = viewModel.Grade;
                courseTraineeToUpdate.CertificateNumber = viewModel.CertificateNumber;
                courseTraineeToUpdate.CertificateUrl = viewModel.CertificateUrl;
                courseTraineeToUpdate.CertificateIssueDate = viewModel.CertificateIssueDate;
                courseTraineeToUpdate.Notes = viewModel.Notes;
                // UpdateDate, etc.

                try
                {
                    _context.Update(courseTraineeToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تحديث بيانات تسجيل المتدرب بنجاح!";
                    return RedirectToAction(nameof(Details), new { id = courseTraineeToUpdate.CourseDetails.CourseId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    // معالجة خطأ التزامن
                    ModelState.AddModelError("", "لم يتم حفظ البيانات. ربما تم تعديلها بواسطة مستخدم آخر.");
                }
            }
            // إذا فشل، أعد ملء بعض البيانات للعرض
            var courseTrainee = await _context.CourseTrainees
                                        .Include(ct => ct.Trainee)
                                        .Include(ct => ct.CourseDetails)
                                        .ThenInclude(cd => cd.Course)
                                        .AsNoTracking() // مهم عند إعادة عرض الفورم بعد فشل التحديث
                                        .FirstOrDefaultAsync(ct => ct.Id == viewModel.CourseTraineeId);
            if (courseTrainee != null)
            {
                viewModel.CourseName = courseTrainee.CourseDetails.Course?.Name ?? "N/A";
                viewModel.TraineeName = courseTrainee.Trainee?.ArName ?? "N/A";
            }
            return View(viewModel);
        }


        // POST: /RemoveEnrollment/{courseTraineeId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEnrollment(Guid courseTraineeId) // قد تستقبل CourseDetailsId أيضاً للredirect
        {
            var courseTrainee = await _context.CourseTrainees.FindAsync(courseTraineeId);
            if (courseTrainee == null)
            {
                return NotFound();
            }

            var courseDetailsId = courseTrainee.CourseDetailsId; // احفظ الـ ID للـ redirect

            var CourseDetails = await _context.CourseDetails.FindAsync(courseDetailsId);
          
            
            _context.CourseTrainees.Remove(courseTrainee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إلغاء تسجيل المتدرب بنجاح.";
            return RedirectToAction(nameof(Details), new { id = CourseDetails.CourseId });
        }


    }
}