using App.BLL.Services;
using App.DAL.EF;
using App.Domain.Enums;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Areas.Admin.Controllers;

public class ProductsController(IAdminProductService productService, AppDbContext db, IWebHostEnvironment env) : AdminBaseController
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

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
    public async Task<IActionResult> AddImage(Guid productId, AdminProductImageDto dto, IFormFile? imageFile)
    {
        try
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                dto.Url = await SaveProductImageAsync(imageFile);
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        if (string.IsNullOrWhiteSpace(dto.Url))
        {
            TempData["Error"] = "Please provide an image file or URL.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        await productService.AddImageAsync(productId, dto);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId)
    {
        var product = await productService.GetByIdAsync(productId);
        var image = product?.Images.FirstOrDefault(i => i.Id == imageId);

        if (image != null)
        {
            DeleteLocalProductImage(image.Url);
        }

        await productService.DeleteImageAsync(productId, imageId);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    private async Task<string> SaveProductImageAsync(IFormFile imageFile)
    {
        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported image file type.");
        }

        var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private void DeleteLocalProductImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(env.WebRootPath, relativePath);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
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
