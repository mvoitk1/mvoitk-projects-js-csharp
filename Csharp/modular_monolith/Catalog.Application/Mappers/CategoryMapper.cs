using Catalog.Domain;
using Modules.SharedKernel;
using Catalog.Application.Dtos.Admin;
using Catalog.Application.Dtos.Categories;

namespace Catalog.Application.Mappers;

public static class CategoryMapper
{
    public static CategoryDto ToDto(Category e) => new()
    {
        Id = e.Id,
        Name = e.Name.Tr(),
        ParentCategoryId = e.ParentCategoryId,
        SubCategories = e.SubCategories?.Select(ToDto).ToList() ?? []
    };

    public static AdminCategoryDto ToAdminDto(Category e) => new()
    {
        Id = e.Id,
        NameEn = e.Name.Lang("en"),
        NameEt = e.Name.Lang("et"),
        ParentCategoryId = e.ParentCategoryId,
        ParentCategoryName = e.ParentCategory?.Name.Tr()
    };

    public static void ApplyWrite(AdminCategoryWriteDto dto, Category entity)
    {
        entity.Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt };
        entity.ParentCategoryId = dto.ParentCategoryId;
    }
}
