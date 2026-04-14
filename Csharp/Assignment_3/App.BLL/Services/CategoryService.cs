using App.DAL.EF;
using App.DTO.v1.Categories;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await db.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null)
            .ToListAsync();

        return categories.Select(MapToDto);
    }

    private static CategoryDto MapToDto(App.Domain.Category c) => new CategoryDto
    {
        Id = c.Id,
        Name = c.Name.Translate() ?? string.Empty,
        ParentCategoryId = c.ParentCategoryId,
        SubCategories = c.SubCategories?
            .Select(MapToDto)
            .ToList() ?? []
    };
}
