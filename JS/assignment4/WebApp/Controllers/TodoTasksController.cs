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
    public class TodoTasksController : Controller
    {
        private readonly AppDbContext _context;

        public TodoTasksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TodoTasks
        public async Task<IActionResult> Index()
        {
            var res = await _context
                .TodoTasks
                .Include(t => t.TodoCategory)
                .Include(t => t.TodoPriority)
                .Where(t =>
                    t.TodoCategory!.AppUserId == User.GetUserId() &&
                    t.TodoPriority!.AppUserId == User.GetUserId()
                )
                .OrderBy(x => x.TaskSort)
                .ThenBy(x => x.CreatedDt)
                .ToListAsync();
            return View(res);
        }

        // GET: TodoTasks/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoTask = await _context.TodoTasks
                .Include(t => t.TodoCategory)
                .Include(t => t.TodoPriority)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.TodoCategory!.AppUserId == User.GetUserId() &&
                    t.TodoPriority!.AppUserId == User.GetUserId()
                );
            if (todoTask == null)
            {
                return NotFound();
            }

            return View(todoTask);
        }

        // GET: TodoTasks/Create
        public IActionResult Create()
        {
            ViewData["TodoCategoryId"] =
                new SelectList(
                    _context.TodoCategories.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.CategorySort),
                    "Id", "CategoryName");
            ViewData["TodoPriorityId"] =
                new SelectList(
                    _context.TodoPriorities.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.PrioritySort),
                    "Id", "PriorityName");
            return View();
        }

        // POST: TodoTasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TodoTask todoTask)
        {
            if (ModelState.IsValid)
            {
                if (await _context.TodoCategories.AnyAsync(x =>
                        x.Id == todoTask.TodoCategoryId && x.AppUserId == User.GetUserId()) &&
                    await _context.TodoPriorities.AnyAsync(x =>
                        x.Id == todoTask.TodoPriorityId && x.AppUserId == User.GetUserId()))
                {
                    _context.Add(todoTask);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Ownership check failed on Priority or Category");
            }

            ViewData["TodoCategoryId"] =
                new SelectList(
                    _context.TodoCategories.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.CategorySort),
                    "Id", "CategoryName", todoTask.TodoCategoryId);
            ViewData["TodoPriorityId"] =
                new SelectList(
                    _context.TodoPriorities.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.PrioritySort),
                    "Id", "PriorityName", todoTask.TodoPriorityId);


            return View(todoTask);
        }

        // GET: TodoTasks/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoTask = await _context.TodoTasks.FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.TodoCategory!.AppUserId == User.GetUserId() &&
                t.TodoPriority!.AppUserId == User.GetUserId()
            );
            if (todoTask == null)
            {
                return NotFound();
            }

            ViewData["TodoCategoryId"] =
                new SelectList(
                    _context.TodoCategories.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.CategorySort),
                    "Id", "CategoryName", todoTask.TodoCategoryId);
            ViewData["TodoPriorityId"] =
                new SelectList(
                    _context.TodoPriorities.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.PrioritySort),
                    "Id", "PriorityName", todoTask.TodoPriorityId);

            return View(todoTask);
        }

        // POST: TodoTasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TodoTask todoTask)
        {
            if (id != todoTask.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (await _context.TodoCategories.AnyAsync(x =>
                        x.Id == todoTask.TodoCategoryId && x.AppUserId == User.GetUserId()) &&
                    await _context.TodoPriorities.AnyAsync(x =>
                        x.Id == todoTask.TodoPriorityId && x.AppUserId == User.GetUserId()) &&
                    await _context.TodoTasks.AnyAsync(x =>
                        x.Id == todoTask.Id && x.TodoCategory!.AppUserId == User.GetUserId() &&
                        x.TodoPriority!.AppUserId == User.GetUserId())
                )
                {
                    _context.Update(todoTask);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Ownership check failed on Task or Priority or Category");
            }

            ViewData["TodoCategoryId"] =
                new SelectList(
                    _context.TodoCategories.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.CategorySort),
                    "Id", "CategoryName", todoTask.TodoCategoryId);
            ViewData["TodoPriorityId"] =
                new SelectList(
                    _context.TodoPriorities.Where(x => x.AppUserId == User.GetUserId()).OrderBy(x => x.PrioritySort),
                    "Id", "PriorityName", todoTask.TodoPriorityId);
            return View(todoTask);
        }

        // GET: TodoTasks/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoTask = await _context.TodoTasks.FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.TodoCategory!.AppUserId == User.GetUserId() &&
                t.TodoPriority!.AppUserId == User.GetUserId()
            );
            if (todoTask == null)
            {
                return NotFound();
            }

            return View(todoTask);
        }

        // POST: TodoTasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var todoTask = await _context.TodoTasks.FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.TodoCategory!.AppUserId == User.GetUserId() &&
                t.TodoPriority!.AppUserId == User.GetUserId()
            );
            if (todoTask == null)
            {
                return NotFound();
            }

            _context.TodoTasks.Remove(todoTask);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}