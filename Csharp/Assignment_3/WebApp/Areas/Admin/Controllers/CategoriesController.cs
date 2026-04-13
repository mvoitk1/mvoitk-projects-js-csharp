using App.BLL.Services;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.Controllers;

public class CategoriesController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
        => View(await catalogueService.GetAllCategoriesAsync());

    public async Task<IActionResult> Create()
    {
        await PopulateParentSelectAsync();
        return View(new AdminCategoryWriteDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCategoryWriteDto dto)
    {
        if (!ModelState.IsValid) { await PopulateParentSelectAsync(); return View(dto); }
        await catalogueService.CreateCategoryAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var cat = await catalogueService.GetCategoryByIdAsync(id);
        if (cat == null) return NotFound();
        await PopulateParentSelectAsync(id);
        return View(new AdminCategoryWriteDto { NameEn = cat.NameEn, NameEt = cat.NameEt, ParentCategoryId = cat.ParentCategoryId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminCategoryWriteDto dto)
    {
        if (!ModelState.IsValid) { await PopulateParentSelectAsync(id); return View(dto); }
        await catalogueService.UpdateCategoryAsync(id, dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await catalogueService.DeleteCategoryAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentSelectAsync(Guid? excludeId = null)
    {
        var all = await catalogueService.GetAllCategoriesAsync();
        var items = all
            .Where(c => c.Id != excludeId)
            .Select(c => new { c.Id, Name = c.NameEn });
        ViewData["Parents"] = new SelectList(items, "Id", "Name");
    }
}
