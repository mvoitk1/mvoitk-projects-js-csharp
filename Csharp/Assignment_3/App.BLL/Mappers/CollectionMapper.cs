using App.Domain;
using App.BLL.DTO.Admin;
using App.BLL.DTO.Collections;

namespace App.BLL.Mappers;

public static class CollectionMapper
{
    public static CollectionDto ToDto(Collection e) => new()
    {
        Id = e.Id,
        Name = e.Name.Tr(),
        Description = e.Description.Tr(),
        LaunchDate = e.LaunchDate,
        IsActive = e.IsActive
    };

    public static AdminCollectionDto ToAdminDto(Collection e) => new()
    {
        Id = e.Id,
        NameEn = e.Name.Lang("en"),
        NameEt = e.Name.Lang("et"),
        DescriptionEn = e.Description.Lang("en"),
        DescriptionEt = e.Description.Lang("et"),
        LaunchDate = e.LaunchDate,
        IsActive = e.IsActive
    };

    public static void ApplyWrite(AdminCollectionWriteDto dto, Collection entity)
    {
        entity.Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt };
        entity.Description = new LangStr(dto.DescriptionEn, "en") { ["et"] = dto.DescriptionEt };
        entity.LaunchDate = dto.LaunchDate;
        entity.IsActive = dto.IsActive;
    }
}
