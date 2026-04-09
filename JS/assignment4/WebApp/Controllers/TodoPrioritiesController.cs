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
    public class TodoPrioritiesController : Controller
    {
        private readonly AppDbContext _context;

        public TodoPrioritiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TodoPriorities
        public async Task<IActionResult> Index()
        {
            var res = await _context.TodoPriorities
                .Where(x => x.AppUserId == User.GetUserId())
                .OrderBy(x => x.PrioritySort)
                .ThenBy(x => x.PriorityName)
                .ToListAsync();

            return View(res);
        }

        // GET: TodoPriorities/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoPriority = await _context
                .TodoPriorities
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());

            if (todoPriority == null)
            {
                return NotFound();
            }

            return View(todoPriority);
        }

        // GET: TodoPriorities/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TodoPriorities/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoPriority todoPriority)
        {
            if (ModelState.IsValid)
            {
                todoPriority.AppUserId = User.GetUserId()!.Value;

                _context.Add(todoPriority);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(todoPriority);
        }

        // GET: TodoPriorities/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoPriority = await _context
                .TodoPriorities
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());
            if (todoPriority == null)
            {
                return NotFound();
            }

            return View(todoPriority);
        }

        // POST: TodoPriorities/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TodoPriority todoPriority)
        {
            if (id != todoPriority.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                todoPriority.AppUserId = User.GetUserId()!.Value;

                _context.Update(todoPriority);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(todoPriority);
        }

        // GET: TodoPriorities/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoPriority = await _context
                .TodoPriorities
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());
            if (todoPriority == null)
            {
                return NotFound();
            }

            var dependentTaskCount = await _context.TodoTasks
                .CountAsync(t => t.TodoPriorityId == id);
            ViewData["DependentTaskCount"] = dependentTaskCount;

            return View(todoPriority);
        }

        // POST: TodoPriorities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var todoPriority = await _context
                .TodoPriorities
                .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == User.GetUserId());
            if (todoPriority == null)
            {
                return NotFound();
            }

            var dependentTaskCount = await _context.TodoTasks
                .CountAsync(t => t.TodoPriorityId == id);
            if (dependentTaskCount > 0)
            {
                ViewData["ErrorMessage"] = $"Cannot delete this priority because it has {dependentTaskCount} dependent task(s). Remove or reassign the tasks first.";
                ViewData["DependentTaskCount"] = dependentTaskCount;
                return View(todoPriority);
            }

            _context.TodoPriorities.Remove(todoPriority);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ViewData["ErrorMessage"] = "Cannot delete this priority because it has dependent task(s). Remove or reassign the tasks first.";
                ViewData["DependentTaskCount"] = 0;
                return View(todoPriority);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}