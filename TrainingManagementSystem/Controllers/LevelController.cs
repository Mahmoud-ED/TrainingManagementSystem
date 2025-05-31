using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;

namespace TrainingManagementSystem.Controllers
{
    public class LevelController : BaseController
    {
        private readonly IUnitOfWork<Level> _levelRepository;

        public LevelController(
             IWebHostEnvironment host,
            IUnitOfWork<Level> levelRepository):base(host) 
        {
            _levelRepository = levelRepository;
        }

        // GET: LevelsDirect
        public IActionResult Index()
        {
            var levels = _levelRepository.Entity.GetAll().ToList(); // ToList() to execute
            return View(levels); // Will look for Views/LevelsDirect/Index.cshtml
                                 // Or you can specify the view: return View("~/Views/Levels/Index.cshtml", levels);
        }

        // GET: LevelsDirect/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var level = await _levelRepository.Entity.GetByIdAsync(id.Value);
            if (level == null)
            {
                return NotFound();
            }
            // return View(level); // Will look for Views/LevelsDirect/Details.cshtml
            return View("~/Views/Levels/Details.cshtml", level);
        }

        // GET: LevelsDirect/Create
        public IActionResult Create()
        {
            // return View(); // Will look for Views/LevelsDirect/Create.cshtml
            return View();
        }

        // POST: LevelsDirect/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Level level)
        {
            if (ModelState.IsValid)
            {
                level.Id = Guid.NewGuid();
                level.Created = DateTime.UtcNow;
                _levelRepository.Entity.Insert(level);
                await _levelRepository.SaveAsync();

                return RedirectToAction(nameof(Index));
            }
            // return View(level);
            return View(level);
        }

        // GET: LevelsDirect/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var level = await _levelRepository.Entity.GetByIdAsync(id.Value);
            if (level == null)
            {
                return NotFound();
            }
            // return View(level);
            return View( level);
        }

        // POST: LevelsDirect/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Created")] Level level)
        {
            if (id != level.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                 
                    var existingLevel = await _levelRepository.Entity.GetByIdAsync(id);
                    if (existingLevel == null)
                    {
                        return NotFound();
                    }

                    // Update only the properties that should be changed
                    existingLevel.Name = level.Name;
                    existingLevel.Modified = DateTime.UtcNow;
                    // existingLevel.Created remains as it was, already preserved by hidden field or not re-binding it.

                    _levelRepository.Entity.Update(existingLevel);
                    await _levelRepository.SaveAsync();
                }
                catch (Exception) // Consider more specific exceptions like DbUpdateConcurrencyException if applicable
                {
                    var levelExists = await _levelRepository.Entity.GetByIdAsync(level.Id) != null;
                    if (!levelExists)
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
            // return View(level);
            return View(level);
        }

        // GET: LevelsDirect/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var level = await _levelRepository.Entity.GetByIdAsync(id.Value);
            if (level == null)
            {
                return NotFound();
            }
            // return View(level);
            return View(level);
        }

        // POST: LevelsDirect/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var level = await _levelRepository.Entity.GetByIdAsync(id);
            if (level != null)
            {
                _levelRepository.Entity.   Delete(level);
                await _levelRepository.SaveAsync();
                // No explicit SaveChangesAsync() call here, assuming IGRepository.Delete handles persistence.
            }
            else
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}