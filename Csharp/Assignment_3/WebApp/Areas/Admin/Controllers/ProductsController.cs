using App.BLL.Services;
using App.DAL.EF;
using App.Domain.Enums;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Areas.Admin.Controllers;

public class ProductsController(IAdminProductService productService, AppDbContext db) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        return View(await productService.GetAllAsync());
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectListsAsync();
        return View(new AdminProductWriteDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductWriteDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(dto);
        }
        var created = await productService.CreateAsync(dto);
        return RedirectToAction(nameof(Edit), new { id = created.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product == null) return NotFound();

        await PopulateSelectListsAsync();
        ViewData["Product"] = product;

        var writeDto = new AdminProductWriteDto
        {
            NameEn = product.NameEn,
            NameEt = product.NameEt,
            DescriptionEn = product.DescriptionEn,
            DescriptionEt = product.DescriptionEt,
            MaterialEn = product.MaterialEn,
            MaterialEt = product.MaterialEt,
            Gender = product.Gender,
            IsActive = product.IsActive,
            CollectionId = product.CollectionId,
            CategoryIds = product.CategoryIds
        };
        return View(writeDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminProductWriteDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            ViewData["Product"] = await productService.GetByIdAsync(id);
            return View(dto);
        }
        var updated = await productService.UpdateAsync(id, dto);
        if (updated == null) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await productService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // ─── Variant actions ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(Guid productId, AdminVariantWriteDto dto)
    {
        await productService.AddVariantAsync(productId, dto);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(Guid productId, Guid variantId)
    {
        await productService.DeleteVariantAsync(productId, variantId);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    // ─── Image actions ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImage(Guid productId, AdminProductImageDto dto)
    {
        await productService.AddImageAsync(productId, dto);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId)
    {
        await productService.DeleteImageAsync(productId, imageId);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    private async Task PopulateSelectListsAsync()
    {
        var collections = await db.Collections.ToListAsync();
        ViewData["Collections"] = new SelectList(
            collections.Select(c => new { c.Id, Name = c.Name.Translate() ?? c.Id.ToString() }),
            "Id", "Name");

        var categories = await db.Categories.ToListAsync();
        ViewData["Categories"] = new MultiSelectList(
            categories.Select(c => new { c.Id, Name = c.Name.Translate() ?? c.Id.ToString() }),
            "Id", "Name");

        var colors = await db.Colors.ToListAsync();
        ViewData["Colors"] = new SelectList(
            colors.Select(c => new { c.Id, Name = c.Name.Translate() ?? c.Id.ToString() }),
            "Id", "Name");

        var sizes = await db.Sizes.ToListAsync();
        ViewData["Sizes"] = new SelectList(
            sizes.Select(s => new { s.Id, Name = s.SizeCode }),
            "Id", "Name");

        ViewData["Genders"] = new SelectList(Enum.GetNames<Gender>());
    }
}
