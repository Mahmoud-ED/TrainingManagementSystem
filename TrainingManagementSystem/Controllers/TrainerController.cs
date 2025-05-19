using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace TrainingManagementSystem.Controllers
{

    [ViewLayout("_LayoutDashboard")]
    public class TrainerController : BaseController
    {

        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Trainer > _trainer;
        private readonly IUnitOfWork<Specialization> _specialization;
        private readonly IUnitOfWork<Qualification> _qualification;
        private readonly IUnitOfWork<TrainerSpecialization> _trainerSpecialization;
        //private readonly IWebHostEnvironment _host;

        public TrainerController(
    ApplicationDbContext context,
                         IUnitOfWork<Trainer> trainer,
                         IUnitOfWork<TrainerSpecialization> trainerSpecialization,
                         IUnitOfWork<Qualification> qualification,
                         IUnitOfWork<Specialization> specialization,
                              IWebHostEnvironment host) : base(host)
        {
            _context = context;
            _qualification = qualification;
            _trainer = trainer;
            _trainerSpecialization = trainerSpecialization;
            _specialization = specialization;
        }


        // GET: Trainer
        public async Task<IActionResult> Index(string searchTerm = null)
        {
            IQueryable<Trainer> query = _trainer.Entity
                                        .Include(t => t.Qualification)
                                        .Include(t => t.TrainerSpecializations)
                                            .ThenInclude(ts => ts.Specialization)
                                        .AsNoTracking();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.ArName.Contains(searchTerm) ||
                                         (t.EnName != null && t.EnName.Contains(searchTerm)) ||
                                         t.Email.Contains(searchTerm) ||
                                         t.PhoneNo.Contains(searchTerm));
            }

            var trainers = await query.OrderBy(t => t.ArName).ToListAsync();

            var trainerVMs = trainers.Select(t => new TrainerVM
            {
                Id = t.Id,
                ArName = t.ArName,
                EnName = t.EnName,
                PhoneNo = t.PhoneNo,
                Email = t.Email,
                NationalNo = t.NationalNo,
                Employer = t.Employer,
                CVUrl = t.CVUrl, // أو اسم الخاصية الصحيح عندك
                ProfileImageUrl = t.ProfileImageUrl,
                CreatedAt = t.Created // تأكد اسم الخاصية
            }).ToList();

            ViewData["SearchTerm"] = searchTerm;

            return View(trainerVMs);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _trainer.Entity 
                .Include(t => t.Qualification)
                .Include(t => t.TrainerSpecializations)
                    .ThenInclude(ts => ts.Specialization)
                .AsNoTracking() // للقراءة فقط
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // GET: Trainer/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new TrainerVM
            {
                QualificationsList = new SelectList(await _qualification.Entity.GetAll().OrderBy(q => q.Name).ToListAsync(), "Id", "Name"),
                SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().ToListAsync(), "Id", "Name")
            };
            return View(viewModel);
        }

        // POST: Trainer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainerVM viewModel,string? isImg1)
        {
            if (ModelState.IsValid)
            {
                var trainer = new Trainer
                {
                    Id = Guid.NewGuid(), // UnitOfWork قد يعتني بهذا إذا كان BaseEntity يفعله
                    ArName = viewModel.ArName,
                    EnName = viewModel.EnName,
                    PhoneNo = viewModel.PhoneNo,
                    Email = viewModel.Email,
                    NationalNo = viewModel.NationalNo,
                    ProfileImageUrl = viewModel.ProfileImageUrl,
                    Employer = viewModel.Employer,
                    QualificationId = viewModel.QualificationId,
                    UserId = viewModel.UserId, // إذا كنت ستعينه
                    Created = DateTime.UtcNow // أو حسب منطق BaseEntity
                };

                if (viewModel.CVFile != null)
                {
                    // استخدم FileHelper من BaseController أو قم بإنشائه
                    //string folder = "uploads/trainers_cvs/";
                    //trainer.CVUrl = await UploadFile(viewModel.CVFile, folder);
                }

                if (viewModel.ProfileImageUrl != null)
                {
                    // استخدم FileHelper من BaseController أو قم بإنشائه
                    //string folder = "uploads/trainers_cvs/";
                    //trainer.CVUrl = await UploadFile(viewModel.CVFile, folder);
                    string? fileName = UploadFile("trainers", viewModel.ProfileImageFile, viewModel.ProfileImageUrl, isImg1);

                }

                _trainer.Entity.Insert(trainer);
                await _trainer.SaveAsync(); // أو _context.SaveChangesAsync() إذا كان UoW لا يفعل ذلك

                // إضافة التخصصات
                if (viewModel.SelectedSpecializationIds != null && viewModel.SelectedSpecializationIds.Any())
                {
                    foreach (var specId in viewModel.SelectedSpecializationIds)
                    {
                        var trainerSpec = new TrainerSpecialization
                        {
                            TrainerId = trainer.Id,
                            SpecializationId = specId
                        };
                         _trainerSpecialization.Entity .Insert(trainerSpec);
                    }
                    await _trainerSpecialization.SaveAsync(); // أو _context.SaveChangesAsync()
                }

                TempData["SuccessMessage"] = "تمت إضافة المدرب بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            // إذا فشل التحقق، أعد ملء القوائم المنسدلة
            viewModel.QualificationsList = new SelectList(await _context.Qualifications.OrderBy(q => q.Name).ToListAsync(), "Id", "Name", viewModel.QualificationId);
            viewModel.SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", viewModel.SelectedSpecializationIds);
            return View(viewModel);
        }

        // GET: Trainer/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _trainer.Entity.GetByIdAsync(id.Value);
            if (trainer == null)
            {
                return NotFound();
            }

            // جلب التخصصات الحالية للمدرب
            var currentSpecializationIds = await _trainerSpecialization.Entity
                                                 .GetWhere(ts => ts.TrainerId == id.Value)
                                                 .Select(ts => ts.SpecializationId)
                                                 .ToListAsync();
            // أو من خلال _trainerSpecializationUoW.GetAllAsync(filter: ts => ts.TrainerId == id.Value) ...

            var viewModel = new TrainerVM
            {
                Id = trainer.Id,
                ArName = trainer.ArName,
                EnName = trainer.EnName,
                PhoneNo = trainer.PhoneNo,
                Email = trainer.Email,
                NationalNo = trainer.NationalNo,
                Employer = trainer.Employer,
                QualificationId = trainer.QualificationId,
                CVUrl = trainer.CVUrl,
                UserId = trainer.UserId,
                CreatedAt = trainer.Created,
                SelectedSpecializationIds = currentSpecializationIds,
                QualificationsList = new SelectList(await _context.Qualifications.OrderBy(q => q.Name).ToListAsync(), "Id", "Name", trainer.QualificationId),
                SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", currentSpecializationIds)
            };

            return View(viewModel);
        }

        // POST: Trainer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TrainerVM viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var trainerToUpdate = await _trainer.Entity.GetByIdAsync(id);
                    if (trainerToUpdate == null)
                    {
                        return NotFound();
                    }

                    trainerToUpdate.ArName = viewModel.ArName;
                    trainerToUpdate.EnName = viewModel.EnName;
                    trainerToUpdate.PhoneNo = viewModel.PhoneNo;
                    trainerToUpdate.Email = viewModel.Email;
                    trainerToUpdate.NationalNo = viewModel.NationalNo;
                    trainerToUpdate.Employer = viewModel.Employer;
                    trainerToUpdate.QualificationId = viewModel.QualificationId;
                    trainerToUpdate.UserId = viewModel.UserId;
                    // CreatedAt لا يجب تعديلها هنا، UpdatedAt إذا كانت موجودة في BaseEntity
                    trainerToUpdate.Modified = DateTime.UtcNow; // افترض وجودها

                    if (viewModel.CVFile != null)
                    {
                        //string folder = "uploads/trainers_cvs/";
                        // حذف الملف القديم إذا وجد وقمت بتحميل ملف جديد
                        if (!string.IsNullOrEmpty(trainerToUpdate.CVUrl))
                        {
                            //DeleteFile(trainerToUpdate.CVUrl, folder); // افترض وجود هذه الدالة في BaseController
                        }
                        //trainerToUpdate.CVUrl = await UploadFile(viewModel.CVFile, folder);
                    }

                    _trainer.Entity.Update(trainerToUpdate); // Update method في UoW يجب أن تعدل حالة الـ entity

                    // تحديث التخصصات (حذف القديم وإضافة الجديد)
                    var existingTrainerSpecs = await _context.TrainerSpecializations
                                                     .Where(ts => ts.TrainerId == id)
                                                     .ToListAsync();
                    // أو من _trainerSpecializationUoW.GetAllAsync(filter: ts => ts.TrainerId == id)

                    foreach (var oldSpec in existingTrainerSpecs)
                    {
                        _trainerSpecialization.Entity.Delete(oldSpec);
                    }

                    if (viewModel.SelectedSpecializationIds != null && viewModel.SelectedSpecializationIds.Any())
                    {
                        foreach (var specId in viewModel.SelectedSpecializationIds)
                        {
                            var newTrainerSpec = new TrainerSpecialization
                            {
                                TrainerId = trainerToUpdate.Id,
                                SpecializationId = specId
                            };
                             _trainerSpecialization.Entity.Insert(newTrainerSpec);
                        }
                    }

                    await _trainerSpecialization.SaveAsync(); // حفظ كل التغييرات
                    await _trainer.SaveAsync(); // حفظ كل التغييرات
                    TempData["SuccessMessage"] = "تم تعديل بيانات المدرب بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TrainerExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // إذا فشل التحقق، أعد ملء القوائم المنسدلة
            viewModel.QualificationsList = new SelectList(await _context.Qualifications.OrderBy(q => q.Name).ToListAsync(), "Id", "Name", viewModel.QualificationId);
            viewModel.SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", viewModel.SelectedSpecializationIds);
            return View(viewModel);
        }

        // GET: Trainer/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // استخدام _context هنا أسهل لـ .Include
            var trainer = await _context.Trainers
                .Include(t => t.Qualification)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // POST: Trainer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var trainer = await _trainer.Entity.GetByIdAsync(id);
            if (trainer == null)
            {
                TempData["ErrorMessage"] = "المدرب غير موجود.";
                return RedirectToAction(nameof(Index));
            }

            // حذف التخصصات المرتبطة أولاً (إذا كان لديك Cascade delete configured في قاعدة البيانات قد لا تحتاج لهذا)
            var trainerSpecializations = await _trainerSpecialization.Entity
                                               .GetWhere(ts => ts.TrainerId == id)
                                               .ToListAsync();
            foreach (var ts in trainerSpecializations)
            {
                _trainerSpecialization.Entity.Delete(ts);
            }
            // لا تنس حفظ التغييرات هنا إذا كان UoW يتطلب ذلك لكل عملية

            // حذف ملف السيرة الذاتية
            if (!string.IsNullOrEmpty(trainer.CVUrl))
            {
                //string folder = "uploads/trainers_cvs/";
                //DeleteFile(trainer.CVUrl, folder);
            }

            _trainer.Entity.Delete(trainer);
            await _trainer.SaveAsync(); // حفظ كل التغييرات
            TempData["SuccessMessage"] = "تم حذف المدرب بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> TrainerExists(Guid id)
        {
            return await _trainer.Entity.GetByIdAsync(id) != null;
        }
    }
}
