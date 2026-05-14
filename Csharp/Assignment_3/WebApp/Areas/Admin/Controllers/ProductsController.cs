using App.BLL.Services;
using App.Domain.Enums;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class ProductsController(
    IAdminProductService productService,
    IAdminCatalogueService catalogueService,
    IWebHostEnvironment env) : AdminBaseController
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
        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(await BuildViewModelAsync(vm.Form));

        var created = await productService.CreateAsync(vm.Form);
        return RedirectToAction(nameof(Edit), new { id = created.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product == null) return NotFound();

        var form = new AdminProductWriteDto
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
        return View(await BuildViewModelAsync(form, product));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var product = await productService.GetByIdAsync(id);
            return View(await BuildViewModelAsync(vm.Form, product));
        }
        var updated = await productService.UpdateAsync(id, vm.Form);
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
                dto.Url = await SaveProductImageAsync(imageFile);
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
            DeleteLocalProductImage(image.Url);

        await productService.DeleteImageAsync(productId, imageId);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ProductEditViewModel> BuildViewModelAsync(
        AdminProductWriteDto? form = null,
        AdminProductDto? product = null)
    {
        var collections = await catalogueService.GetAllCollectionsAsync();
        var categories = await catalogueService.GetAllCategoriesAsync();
        var colors = await catalogueService.GetAllColorsAsync();
        var sizes = await catalogueService.GetAllSizesAsync();

        return new ProductEditViewModel
        {
            Form = form ?? new AdminProductWriteDto(),
            Product = product,
            Collections = new SelectList(
                collections.Select(c => new { c.Id, Name = c.NameEn }),
                "Id", "Name"),
            Categories = new MultiSelectList(
                categories.Select(c => new { c.Id, Name = c.NameEn }),
                "Id", "Name"),
            Colors = new SelectList(
                colors.Select(c => new { c.Id, c.Name }),
                "Id", "Name"),
            Sizes = new SelectList(
                sizes.Select(s => new { s.Id, s.SizeCode }),
                "Id", "SizeCode"),
            Genders = new SelectList(Enum.GetNames<Gender>()),
        };
    }

    private async Task<string> SaveProductImageAsync(IFormFile imageFile)
    {
        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported image file type.");

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
        if (string.IsNullOrWhiteSpace(imageUrl) ||
            !imageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
            return;

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(env.WebRootPath, relativePath);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }
}
