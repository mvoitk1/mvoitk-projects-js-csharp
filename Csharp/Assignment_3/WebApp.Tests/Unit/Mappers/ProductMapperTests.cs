using App.BLL.Mappers;
using App.Domain;
using App.Domain.Enums;
using Xunit;

namespace WebApp.Tests.Unit.Mappers;

public class ProductMapperTests
{
    [Fact]
    public void ToDto_MapsScalarFieldsAndTranslatesLangStr()
    {
        var product = new Product
        {
            Name = new LangStr("Tee", "en") { ["et"] = "Särk" },
            Description = new LangStr("A tee", "en"),
            Material = new LangStr("Cotton", "en"),
            Gender = Gender.Men,
            IsActive = true
        };

        var dto = ProductMapper.ToDto(product);

        Assert.Equal("Tee", dto.Name);
        Assert.Equal("A tee", dto.Description);
        Assert.Equal("Cotton", dto.Material);
        Assert.Equal("Men", dto.Gender);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void ToDto_NullNavigationCollections_ProduceEmptyLists()
    {
        var product = new Product
        {
            Name = new LangStr("x", "en"),
            Description = new LangStr("x", "en"),
            Material = new LangStr("x", "en")
        };

        var dto = ProductMapper.ToDto(product);

        Assert.Empty(dto.Variants);
        Assert.Empty(dto.Images);
        Assert.Empty(dto.CategoryNames);
    }

    [Fact]
    public void ToDto_OnlyActiveVariantsAreMapped()
    {
        var product = new Product
        {
            Name = new LangStr("x", "en"),
            Description = new LangStr("x", "en"),
            Material = new LangStr("x", "en"),
            Variants = new List<ProductVariant>
            {
                new() { Sku = "A", IsActive = true },
                new() { Sku = "B", IsActive = false }
            }
        };

        var dto = ProductMapper.ToDto(product);

        Assert.Single(dto.Variants);
        Assert.Equal("A", dto.Variants[0].Sku);
    }

    [Fact]
    public void ToListItem_LowestPrice_IgnoresInactiveVariants()
    {
        var product = new Product
        {
            Name = new LangStr("Tee", "en"),
            Description = new LangStr("x", "en"),
            Material = new LangStr("x", "en"),
            Variants = new List<ProductVariant>
            {
                new() { Price = 50m, IsActive = true },
                new() { Price = 20m, IsActive = false },
                new() { Price = 30m, IsActive = true }
            },
            Images = new List<ProductImage>
            {
                new() { Url = "second.jpg", SortOrder = 2 },
                new() { Url = "first.jpg", SortOrder = 1 }
            }
        };

        var dto = ProductMapper.ToListItem(product);

        Assert.Equal(30m, dto.LowestPrice);
        Assert.Equal("first.jpg", dto.PrimaryImageUrl);
    }

    [Fact]
    public void ToListItem_NoVariantsOrImages_LeavesOptionalFieldsNull()
    {
        var product = new Product
        {
            Name = new LangStr("Tee", "en"),
            Description = new LangStr("x", "en"),
            Material = new LangStr("x", "en")
        };

        var dto = ProductMapper.ToListItem(product);

        Assert.Null(dto.LowestPrice);
        Assert.Null(dto.PrimaryImageUrl);
    }
}
