using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities; // Your DbContext and Entities

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    [Display(Name = "تصنيفات البرامج التدريبية")] // ✅ Controller title

    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                                        .Include(c => c.CourseClassification)
                                        .Include(c => c.Level)
                                        .Include(c => c.CourseParent)
                                        .ToListAsync();
            return View(courses);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .Include(c => c.CourseDetails)
                    .ThenInclude(cd => cd.Status)
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again to chain another .ThenInclude
                    .ThenInclude(cd => cd.Location) // **** IF Location IS AN ENTITY ****
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again
                    .ThenInclude(cd => cd.CourseTrainees)
                .Include(c => c.CourseTrainers)
                    .ThenInclude(ct => ct.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }
        public async Task<IActionResult> SessionDetails(Guid id) // id here is CourseDetails.Id
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var courseDetailItem = await _context.CourseDetails
                                        .Include(cd => cd.Status)
                                        .Include(cd => cd.Course) // To show parent course info if needed
                                        .Include(cd => cd.CourseTrainees)
                                            .ThenInclude(cst => cst.Trainee) // To list trainees of this session
                                        .FirstOrDefaultAsync(cd => cd.Id == id);

            if (courseDetailItem == null)
            {
                return NotFound();
            }
            return View(courseDetailItem);
        }


        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseClassificationId,LevelId,Name,Code,Description,CourseParentId")] Course course)
        {
            //if (ModelState.IsValid)
            //{
                course.Created = DateTime.Now; 
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}
            //await PopulateDropdownsAsync(course);
            //return View(course);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .Include(c => c.CourseDetails)
                    .ThenInclude(cd => cd.Status)
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again to chain another .ThenInclude
                    .ThenInclude(cd => cd.Location) // **** IF Location IS AN ENTITY ****
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again
                    .ThenInclude(cd => cd.CourseTrainees)
                .Include(c => c.CourseTrainers)
                    .ThenInclude(ct => ct.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }
            await PopulateDropdownsAsync(course);
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CourseClassificationId,LevelId,Name,Code,Description,CourseParentId,CreatedAt")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
                try
                {
                    course.Modified = DateTime.UtcNow;
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            //}
            //await PopulateDropdownsAsync(course);
            //return View(course);
        }

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

            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        private async Task PopulateDropdownsAsync(Course course = null)
        {
            ViewBag.CourseClassificationId = new SelectList(await _context.CourseClassifications.OrderBy(cc => cc.Name).ToListAsync(), "Id", "Name", course?.CourseClassificationId);
            ViewBag.LevelId = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", course?.LevelId);
            ViewBag.CourseParentId = new SelectList(await _context.CourseParent.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", course?.CourseParentId);

          
        }
    }
}