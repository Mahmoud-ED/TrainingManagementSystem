using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    public class OrganizitionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // حقن الـ DbContext عبر الـ Constructor
        public OrganizitionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Organizitions (عرض كل الجهات)
        public async Task<IActionResult> Index()
        {
            // نستخدم Include لجلب اسم الفئة (Category) مع كل جهة لتجنب N+1 problem
            var organizitions = await _context.Organizitions.Include(o => o.Category).ToListAsync();
            return View(organizitions);
        }

        // GET: Organizitions/Details/5 (عرض تفاصيل جهة معينة)
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // جلب الجهة مع الفئة والمتدربين المرتبطين بها
            var organizition = await _context.Organizitions
                .Include(o => o.Category)
                .Include(o => o.Trainees) // لجلب قائمة المتدربين
                .FirstOrDefaultAsync(m => m.Id == id);

            if (organizition == null)
            {
                return NotFound();
            }

            return View(organizition);
        }

        // GET: Organizitions/Create (عرض فورم إنشاء جهة جديدة)
        public IActionResult Create()
        {
            // تجهيز قائمة الفئات (Categories) لعرضها في Dropdown list
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Organizitions/Create (حفظ الجهة الجديدة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CategoryId,NUM,ChiefName,ChiefTitle,PhoneNo,Email,StreetAddress")] Organizition organizition)
        {
            // إزالة Category من الـ ModelState لأننا لا نرسلها من الفورم مباشرة
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                organizition.Id = Guid.NewGuid();
                _context.Add(organizition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // في حال وجود خطأ، أعد تحميل قائمة الفئات
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", organizition.CategoryId);
            return View(organizition);
        }

        // GET: Organizitions/Edit/5 (عرض فورم تعديل جهة)
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizition = await _context.Organizitions.FindAsync(id);
            if (organizition == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", organizition.CategoryId);
            return View(organizition);
        }

        // POST: Organizitions/Edit/5 (حفظ التعديلات)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,NUM,CategoryId,ChiefName,ChiefTitle,PhoneNo,Email,StreetAddress")] Organizition organizition)
        {
            if (id != organizition.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(organizition);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizitionExists(organizition.Id))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", organizition.CategoryId);
            return View(organizition);
        }

        // GET: Organizitions/Delete/5 (عرض صفحة تأكيد الحذف)
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizition = await _context.Organizitions
                .Include(o => o.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (organizition == null)
            {
                return NotFound();
            }

            return View(organizition);
        }

        // POST: Organizitions/Delete/5 (تنفيذ الحذف)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var organizition = await _context.Organizitions.FindAsync(id);
            if (organizition != null)
            {
                _context.Organizitions.Remove(organizition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrganizitionExists(Guid id)
        {
            return _context.Organizitions.Any(e => e.Id == id);
        }
    }
}
