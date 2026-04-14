using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain.ApiKey.SimpleList;
using com.akaver.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Controllers
{
    [Authorize]
    public class ListItemsController : Controller
    {
        private readonly AppDbContext _context;

        public ListItemsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ListItems
        public async Task<IActionResult> Index(bool? completed)
        {
            var query = _context.ListItems
                .Include(l => l.ApiKey)
                .Where(l => l.ApiKey!.AppUserId == User.GetUserId())
                .AsQueryable();
            if (completed != null)
            {
                query = query.Where(l => l.Completed == completed);
            }

            return View(await query.ToListAsync());

        }

        // GET: ListItems/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listItem = await _context.ListItems
                .Include(l => l.ApiKey)
                .FirstOrDefaultAsync(m => m.Id == id && m.ApiKey!.AppUserId == User.GetUserId());
            if (listItem == null)
            {
                return NotFound();
            }

            return View(listItem);

        }

        // GET: ListItems/Create
        public async Task<IActionResult> Create()
        {
            ViewData["ApiKeyId"] = new SelectList(await _context.ApiKeys.Where(m => m.AppUserId == User.GetUserId()).ToListAsync(), "Id", "AppName");
            return View();
        }

        // POST: ListItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListItem listItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(listItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ApiKeyId"] = new SelectList(await _context.ApiKeys.Where(m => m.AppUserId == User.GetUserId()).ToListAsync(), "Id", "AppName", listItem.ApiKeyId);
            return View(listItem);

        }

        // GET: ListItems/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listItem = await _context.ListItems.FindAsync(id);
            if (listItem == null)
            {
                return NotFound();
            }
            ViewData["ApiKeyId"] = new SelectList(await _context.ApiKeys.Where(m => m.AppUserId == User.GetUserId()).ToListAsync(), "Id", "AppName", listItem.ApiKeyId);
            return View(listItem);

        }

        // POST: ListItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ListItem listItem)
        {
            if (id != listItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(listItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ListItemExists(listItem.Id))
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
            ViewData["ApiKeyId"] = new SelectList(await _context.ApiKeys.Where(m => m.AppUserId == User.GetUserId()).ToListAsync(), "Id", "AppName", listItem.ApiKeyId);
            return View(listItem);

        }

        // GET: ListItems/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listItem = await _context.ListItems
                .Include(l => l.ApiKey)
                .FirstOrDefaultAsync(m => m.Id == id && m.ApiKey!.AppUserId == User.GetUserId());
            if (listItem == null)
            {
                return NotFound();
            }

            return View(listItem);

        }

        // POST: ListItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var listItem = await _context.ListItems.FirstOrDefaultAsync(m => m.Id == id && m.ApiKey!.AppUserId == User.GetUserId());
            if (listItem == null) return NotFound();
            _context.ListItems.Remove(listItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        private bool ListItemExists(Guid id)
        {
            return _context.ListItems.Any(e => e.Id == id);
        }
    }
}
