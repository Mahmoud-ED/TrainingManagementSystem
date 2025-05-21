using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.IO; // Added for Path

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    public class TrainerController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Trainer> _trainer;
        private readonly IUnitOfWork<Specialization> _specialization;
        private readonly IUnitOfWork<Qualification> _qualification;
        private readonly IUnitOfWork<TrainerSpecialization> _trainerSpecialization;

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
                CVUrl = t.CVUrl,
                ProfileImageUrl = t.ProfileImageUrl,
                CreatedAt = t.Created
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
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null)
            {
                return NotFound();
            }
            return View(trainer);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new TrainerVM
            {
                QualificationsList = new SelectList(await _qualification.Entity.GetAll().OrderBy(q => q.Name).ToListAsync(), "Id", "Name"),
                SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().ToListAsync(), "Id", "Name")
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainerVM viewModel, string? isImg1)
        {
            if (ModelState.IsValid)
            {
                var trainer = new Trainer
                {
                    Id = Guid.NewGuid(),
                    ArName = viewModel.ArName,
                    EnName = viewModel.EnName,
                    PhoneNo = viewModel.PhoneNo,
                    Email = viewModel.Email,
                    NationalNo = viewModel.NationalNo,
                    ProfileImageUrl = viewModel.ProfileImageUrl,
                    Employer = viewModel.Employer,
                    QualificationId = viewModel.QualificationId,
                    UserId = viewModel.UserId,
                    Created = DateTime.UtcNow
                };

                if (viewModel.CVFile != null)
                {
                    // string folder = "uploads/trainers_cvs/";
                    // trainer.CVUrl = await UploadFile(viewModel.CVFile, folder);
                }

                if (viewModel.ProfileImageFile != null) // Changed from ProfileImageUrl to ProfileImageFile for condition
                {
                    string? fileName = UploadFile("trainers", viewModel.ProfileImageFile, viewModel.ProfileImageUrl, isImg1);
                    trainer.ProfileImageUrl = fileName; // Assuming UploadFile returns the new filename to be stored
                }


                _trainer.Entity.Insert(trainer);
                await _trainer.SaveAsync();

                if (viewModel.SelectedSpecializationIds != null && viewModel.SelectedSpecializationIds.Any())
                {
                    foreach (var specId in viewModel.SelectedSpecializationIds)
                    {
                        var trainerSpec = new TrainerSpecialization
                        {
                            TrainerId = trainer.Id,
                            SpecializationId = specId
                        };
                        _trainerSpecialization.Entity.Insert(trainerSpec);
                    }
                    await _trainerSpecialization.SaveAsync();
                }

                TempData["SuccessMessage"] = "تمت إضافة المدرب بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            viewModel.QualificationsList = new SelectList(await _context.Qualifications.OrderBy(q => q.Name).ToListAsync(), "Id", "Name", viewModel.QualificationId);
            viewModel.SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", viewModel.SelectedSpecializationIds);
            return View(viewModel);
        }

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

            var currentSpecializationIds = await _trainerSpecialization.Entity
                                                 .GetWhere(ts => ts.TrainerId == id.Value)
                                                 .Select(ts => ts.SpecializationId)
                                                 .ToListAsync();

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
                ProfileImageUrl = trainer.ProfileImageUrl, // Added this line
                UserId = trainer.UserId,
                CreatedAt = trainer.Created,
                SelectedSpecializationIds = currentSpecializationIds,
                QualificationsList = new SelectList(await _context.Qualifications.OrderBy(q => q.Name).ToListAsync(), "Id", "Name", trainer.QualificationId),
                SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", currentSpecializationIds)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TrainerVM viewModel) // This is the first Edit POST
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
                    trainerToUpdate.Modified = DateTime.UtcNow;

                    if (viewModel.CVFile != null)
                    {
                        //string folder = "uploads/trainers_cvs/";
                        if (!string.IsNullOrEmpty(trainerToUpdate.CVUrl))
                        {
                            //DeleteFile(trainerToUpdate.CVUrl, folder);
                        }
                        //trainerToUpdate.CVUrl = await UploadFile(viewModel.CVFile, folder);
                    }
                    // Note: ProfileImageFile handling is missing in this version of Edit Post

                    _trainer.Entity.Update(trainerToUpdate);

                    var existingTrainerSpecs = await _context.TrainerSpecializations
                                                     .Where(ts => ts.TrainerId == id)
                                                     .ToListAsync();
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

                    await _trainerSpecialization.SaveAsync();
                    await _trainer.SaveAsync();
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

            viewModel.QualificationsList = new SelectList(await _context.Qualifications.OrderBy(q => q.Name).ToListAsync(), "Id", "Name", viewModel.QualificationId);
            viewModel.SpecializationsList = new MultiSelectList(await _specialization.Entity.GetAll().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", viewModel.SelectedSpecializationIds);
            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف المدرب غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index)); 
            }

            var trainerViewModel = await _context.Trainers
                .Where(t => t.Id == id.Value) 
                .Select(t => new TrainerVM 
                {
                    Id = t.Id,
                    ArName = t.ArName,
                    EnName = t.EnName, 
                    Email = t.Email,   
                    PhoneNo = t.PhoneNo, 
                    ProfileImageUrl = t.ProfileImageUrl 
                })
                .FirstOrDefaultAsync(); // جلب أول نتيجة مطابقة أو null

            // 3. التحقق مما إذا تم العثور على المدرب
            if (trainerViewModel == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المدرب المطلوب حذفه.";
                return RedirectToAction(nameof(Index)); // إعادة توجيه إلى قائمة المدربين
            }

            // 4. إذا تم العثور على المدرب، قم بتمرير الـ ViewModel إلى الـ View
            return View(trainerViewModel); // سيعرض Views/Trainer/Delete.cshtml ويمرر له trainerViewModel
        }


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

            var trainerSpecializations = await _trainerSpecialization.Entity
                                               .GetWhere(ts => ts.TrainerId == id)
                                               .ToListAsync();
            foreach (var ts in trainerSpecializations)
            {
                _trainerSpecialization.Entity.Delete(ts);
            }

            if (!string.IsNullOrEmpty(trainer.CVUrl))
            {
                //string folder = "uploads/trainers_cvs/";
                //DeleteFile(trainer.CVUrl, folder);
            }
            if (!string.IsNullOrEmpty(trainer.ProfileImageUrl)) // Added to delete profile image
            {
                DeleteFile(trainer.ProfileImageUrl, "trainers");
            }

            _trainer.Entity.Delete(trainer);
            await _trainer.SaveAsync();
            TempData["SuccessMessage"] = "تم حذف المدرب بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> TrainerExists(Guid id)
        {
            return await _trainer.Entity.GetByIdAsync(id) != null;
        }

        private async Task PopulateDropdownsAsync(TrainerVM model)
        {
            model.QualificationsList = new SelectList(
                await _qualification.Entity.GetAll().OrderBy(q => q.Name).ToListAsync(),
                "Id",
                "Name",
                model.QualificationId);

            var allSpecializations = await _specialization.Entity.GetAll()
                                                   .OrderBy(s => s.Name)
                                                   .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                                                   .ToListAsync();
            model.SpecializationsList = new MultiSelectList(allSpecializations, "Value", "Text", model.SelectedSpecializationIds);
        }
        private async Task PopulateCreateDropdownsAsync(TrainerVM model)
        {
            model.QualificationsList = new SelectList(
                await _qualification.Entity.GetAll().OrderBy(q => q.Name).ToListAsync(),
                "Id",
                "Name");

            var allSpecializations = await _specialization.Entity.GetAll()
                                                   .OrderBy(s => s.Name)
                                                   .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                                                   .ToListAsync();
            model.SpecializationsList = new MultiSelectList(allSpecializations, "Value", "Text");
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(Guid id, TrainerVM model, string? isImg1) // This is the second Edit POST
        //{
        //    if (id != model.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (await _context.Trainers.AnyAsync(t => t.Email == model.Email && t.Id != model.Id))
        //    {
        //        ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم مسبقًا لمدرب آخر.");
        //    }
        //    if (await _context.Trainers.AnyAsync(t => t.PhoneNo == model.PhoneNo && t.Id != model.Id))
        //    {
        //        ModelState.AddModelError("PhoneNo", "رقم الهاتف مستخدم مسبقًا لمدرب آخر.");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            var trainerToUpdate = await _context.Trainers
        //                .Include(t => t.TrainerSpecializations)
        //                .FirstOrDefaultAsync(t => t.Id == model.Id);

        //            if (trainerToUpdate == null)
        //            {
        //                return NotFound();
        //            }

        //            trainerToUpdate.ArName = model.ArName;
        //            trainerToUpdate.EnName = model.EnName;
        //            trainerToUpdate.PhoneNo = model.PhoneNo;
        //            trainerToUpdate.Email = model.Email;
        //            trainerToUpdate.NationalNo = model.NationalNo;
        //            trainerToUpdate.Employer = model.Employer;
        //            trainerToUpdate.Modified = DateTime.Now;
        //            trainerToUpdate.QualificationId = model.QualificationId;

        //            string? newProfileImageName = null;
        //            if (model.ProfileImageFile != null)
        //            {
        //                newProfileImageName = UploadFile("trainers", model.ProfileImageFile, trainerToUpdate.ProfileImageUrl, isImg1);
        //                trainerToUpdate.ProfileImageUrl = newProfileImageName;
        //            }
        //            else if (isImg1 == "false" && !string.IsNullOrEmpty(trainerToUpdate.ProfileImageUrl)) // Image removed from UI
        //            {
        //                DeleteFile(trainerToUpdate.ProfileImageUrl, "trainers");
        //                trainerToUpdate.ProfileImageUrl = null;
        //            }
        //            // If ProfileImageFile is null and isImg1 is "true" or null, keep existing image.

        //            string? newCvFileName = null;
        //            if (model.CVFile != null) // Check CVFile not CVUrl
        //            {
        //                newCvFileName = UploadFile("cvs", model.CVFile, trainerToUpdate.CVUrl, null); // Assuming isImg1 is not relevant for CV
        //                trainerToUpdate.CVUrl = newCvFileName;
        //            }
        //            else if (string.IsNullOrEmpty(model.CVUrl) && !string.IsNullOrEmpty(trainerToUpdate.CVUrl)) // CV explicitly cleared from UI
        //            {
        //                DeleteFile(trainerToUpdate.CVUrl, "cvs");
        //                trainerToUpdate.CVUrl = null;
        //            }

        //            var existingTrainerSpecializations = _context.TrainerSpecializations.Where(ts => ts.TrainerId == trainerToUpdate.Id).ToList();
        //            _context.TrainerSpecializations.RemoveRange(existingTrainerSpecializations);

        //            if (model.SelectedSpecializationIds != null && model.SelectedSpecializationIds.Any())
        //            {
        //                foreach (var specId in model.SelectedSpecializationIds)
        //                {
        //                    trainerToUpdate.TrainerSpecializations.Add(new TrainerSpecialization { SpecializationId = specId, TrainerId = trainerToUpdate.Id });
        //                }
        //            }
        //            _context.Update(trainerToUpdate);
        //            await _context.SaveChangesAsync();
        //            TempData["SuccessMessage"] = "تم تعديل بيانات المدرب بنجاح.";
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!await TrainerExists(model.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    await PopulateDropdownsAsync(model);
        //    return View(model);
        //}

        private void DeleteFile(string fileName, string subfolder)
        {
            //if (string.IsNullOrEmpty(fileName)) return;
            //string webRootPath = _host?.WebRootPath ?? string.Empty; // Use _host from BaseController
            //if (string.IsNullOrEmpty(webRootPath))
            //{
            //    // Log error or handle missing webRootPath if necessary, for now, we proceed cautiously
            //    // This path will be relative to the application's current directory if webRootPath is empty
            //}
            //string filePath = Path.Combine(webRootPath, "pictures", subfolder, fileName);
            //if (System.IO.File.Exists(filePath))
            //{
            //    System.IO.File.Delete(filePath);
            //}
        }
    }
}