using App.BLL.Mappers;
using App.DAL.EF.UnitOfWork;
using App.Domain;
using App.DTO.v1.Admin;

namespace App.BLL.Services;

public class AdminProductService(IAppUnitOfWork uow) : IAdminProductService
{
    public async Task<IEnumerable<AdminProductDto>> GetAllAsync()
    {
        var products = await uow.Products.GetAllWithDetailsAsync();
        return products.Select(AdminProductMapper.ToDto);
    }

    public async Task<AdminProductDto?> GetByIdAsync(Guid id)
    {
        var product = await uow.Products.GetWithDetailsAsync(id);
        return product == null ? null : AdminProductMapper.ToDto(product);
    }

    public async Task<AdminProductDto> CreateAsync(AdminProductWriteDto dto)
    {
        var product = new Product();
        AdminProductMapper.ApplyWrite(dto, product);

        if (dto.CategoryIds.Any())
        {
            product.ProductCategories = dto.CategoryIds
                .Select(cId => new ProductCategory { CategoryId = cId })
                .ToList();
        }

        uow.Products.Add(product);
        await uow.SaveChangesAsync();

        return (await GetByIdAsync(product.Id))!;
    }

    public async Task<AdminProductDto?> UpdateAsync(Guid id, AdminProductWriteDto dto)
    {
        var product = await uow.Products.GetWithCategoriesAsync(id);
        if (product == null) return null;

        AdminProductMapper.ApplyWrite(dto, product);

        // Replace categories
        if (product.ProductCategories != null)
            uow.ProductCategories.RemoveRange(product.ProductCategories);

        if (dto.CategoryIds.Any())
        {
            uow.ProductCategories.AddRange(dto.CategoryIds
                .Select(cId => new ProductCategory { ProductId = id, CategoryId = cId }));
        }

        await uow.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await uow.Products.FindAsync(id);
        if (product == null) return false;

        uow.Products.Remove(product);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<AdminVariantDto> AddVariantAsync(Guid productId, AdminVariantWriteDto dto)
    {
        var variant = new ProductVariant { ProductId = productId };
        AdminProductMapper.ApplyWrite(dto, variant);

        uow.ProductVariants.Add(variant);
        await uow.SaveChangesAsync();

        var created = await uow.ProductVariants.GetWithColorAndSizeAsync(variant.Id);
        return AdminProductMapper.ToVariantDto(created!);
    }

    public async Task<AdminVariantDto?> UpdateVariantAsync(Guid productId, Guid variantId, AdminVariantWriteDto dto)
    {
        var variant = await uow.ProductVariants.GetForProductAsync(productId, variantId);
        if (variant == null) return null;

        AdminProductMapper.ApplyWrite(dto, variant);
        await uow.SaveChangesAsync();
        return AdminProductMapper.ToVariantDto(variant);
    }

    public async Task<bool> DeleteVariantAsync(Guid productId, Guid variantId)
    {
        var variant = await uow.ProductVariants.FindForProductAsync(productId, variantId);
        if (variant == null) return false;

        uow.ProductVariants.Remove(variant);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<AdminProductImageDto> AddImageAsync(Guid productId, AdminProductImageDto dto)
    {
        var image = new ProductImage { ProductId = productId };
        AdminProductMapper.ApplyWrite(dto, image);

        uow.ProductImages.Add(image);
        await uow.SaveChangesAsync();

        return AdminProductMapper.ToImageDto(image);
    }

    public async Task<bool> DeleteImageAsync(Guid productId, Guid imageId)
    {
        var image = await uow.ProductImages.FindForProductAsync(productId, imageId);
        if (image == null) return false;

        uow.ProductImages.Remove(image);
        await uow.SaveChangesAsync();
        return true;
    }
}
