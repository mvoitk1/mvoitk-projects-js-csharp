using App.BLL.Services;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Admin.Controllers;

public class CollectionsController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
        => View(await catalogueService.GetAllCollectionsAsync());

    public IActionResult Create() => View(new AdminCollectionWriteDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCollectionWriteDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await catalogueService.CreateCollectionAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var col = await catalogueService.GetCollectionByIdAsync(id);
        if (col == null) return NotFound();
        return View(new AdminCollectionWriteDto
        {
            NameEn = col.NameEn, NameEt = col.NameEt,
            DescriptionEn = col.DescriptionEn, DescriptionEt = col.DescriptionEt,
            LaunchDate = col.LaunchDate, IsActive = col.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminCollectionWriteDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await catalogueService.UpdateCollectionAsync(id, dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await catalogueService.DeleteCollectionAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
