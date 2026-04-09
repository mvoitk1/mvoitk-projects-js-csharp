using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain.Todo;
using com.akaver.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Controllers
{
    [Authorize]
    public class TodoCategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public TodoCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TodoCategories
        public async Task<IActionResult> Index()
        {
            var res = await _context.TodoCategories
                .Where(x => x.AppUserId == User.GetUserId())
                .OrderBy(x => x.CategorySort)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();
            return View(res);
        }

        // GET: TodoCategories/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoCategory = await _context.TodoCategories
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());
            if (todoCategory == null)
            {
                return NotFound();
            }

            return View(todoCategory);
        }

        // GET: TodoCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TodoCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoCategory todoCategory)
        {
            if (ModelState.IsValid)
            {
                todoCategory.AppUserId = User.GetUserId()!.Value;
                _context.Add(todoCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(todoCategory);
        }

        // GET: TodoCategories/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoCategory = await _context
                .TodoCategories
                .FirstOrDefaultAsync(x => x.AppUserId == User.GetUserId() && x.Id == id);
            if (todoCategory == null)
            {
                return NotFound();
            }

            return View(todoCategory);
        }

        // POST: TodoCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TodoCategory todoCategory)
        {
            if (id != todoCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                todoCategory.AppUserId = User.GetUserId()!.Value;

                _context.Update(todoCategory);
                await _context.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }

            return View(todoCategory);
        }

        // GET: TodoCategories/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoCategory = await _context
                .TodoCategories
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());

            if (todoCategory == null)
            {
                return NotFound();
            }

            var dependentTaskCount = await _context.TodoTasks
                .CountAsync(t => t.TodoCategoryId == id);
            ViewData["DependentTaskCount"] = dependentTaskCount;

            return View(todoCategory);
        }

        // POST: TodoCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var todoCategory =
                await _context.TodoCategories.FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());

            if (todoCategory == null)
            {
                return NotFound();
            }

            var dependentTaskCount = await _context.TodoTasks
                .CountAsync(t => t.TodoCategoryId == id);
            if (dependentTaskCount > 0)
            {
                ViewData["ErrorMessage"] = $"Cannot delete this category because it has {dependentTaskCount} dependent task(s). Remove or reassign the tasks first.";
                ViewData["DependentTaskCount"] = dependentTaskCount;
                return View(todoCategory);
            }

            _context.TodoCategories.Remove(todoCategory);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ViewData["ErrorMessage"] = "Cannot delete this category because it has dependent task(s). Remove or reassign the tasks first.";
                ViewData["DependentTaskCount"] = 0;
                return View(todoCategory);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}