using App.BLL.Services;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class CategoriesController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
        => View(await catalogueService.GetAllCategoriesAsync());

    public async Task<IActionResult> Create()
        => View(await BuildViewModelAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(await BuildViewModelAsync(vm.Form));

        await catalogueService.CreateCategoryAsync(vm.Form);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var cat = await catalogueService.GetCategoryByIdAsync(id);
        if (cat == null) return NotFound();

        var form = new AdminCategoryWriteDto
        {
            NameEn = cat.NameEn,
            NameEt = cat.NameEt,
            ParentCategoryId = cat.ParentCategoryId
        };
        return View(await BuildViewModelAsync(form, id));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(await BuildViewModelAsync(vm.Form, id));

        await catalogueService.UpdateCategoryAsync(id, vm.Form);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await catalogueService.DeleteCategoryAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<CategoryFormViewModel> BuildViewModelAsync(
        AdminCategoryWriteDto? form = null,
        Guid? excludeId = null)
    {
        var all = await catalogueService.GetAllCategoriesAsync();
        var items = all
            .Where(c => c.Id != excludeId)
            .Select(c => new { c.Id, Name = c.NameEn });
        return new CategoryFormViewModel
        {
            Form = form ?? new AdminCategoryWriteDto(),
            ParentOptions = new SelectList(items, "Id", "Name")
        };
    }
}
