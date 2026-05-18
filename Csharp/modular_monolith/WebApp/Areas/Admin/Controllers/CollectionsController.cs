using Catalog.Application.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using BllAdmin = Catalog.Application.Dtos.Admin;

namespace WebApp.Areas.Admin.Controllers;

public class CollectionsController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        var collections = await catalogueService.GetAllCollectionsAsync();
        return View(collections.MapList<AdminCollectionDto>());
    }

    public IActionResult Create() => View(new AdminCollectionWriteDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCollectionWriteDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await catalogueService.CreateCollectionAsync(dto.MapTo<BllAdmin.AdminCollectionWriteDto>());
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
        await catalogueService.UpdateCollectionAsync(id, dto.MapTo<BllAdmin.AdminCollectionWriteDto>());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await catalogueService.DeleteCollectionAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
