using App.BLL.Mappers;
using App.Domain;
using App.Domain.Enums;
using Xunit;

namespace WebApp.Tests.Unit.Mappers;

public class CartMapperTests
{
    [Fact]
    public void ToDto_NullItems_ProducesEmptyList()
    {
        var cart = new Cart { Status = CartStatus.Active };

        var dto = CartMapper.ToDto(cart);

        Assert.Empty(dto.Items);
        Assert.Equal("Active", dto.Status);
    }

    [Fact]
    public void ToItemDto_NullProductVariant_FallsBackToEmptyStrings()
    {
        var item = new CartItem { Quantity = 1, UnitPrice = 5m };

        var dto = CartMapper.ToItemDto(item);

        Assert.Equal(string.Empty, dto.ProductName);
        Assert.Equal(string.Empty, dto.Sku);
        Assert.Equal(string.Empty, dto.ColorName);
        Assert.Equal(string.Empty, dto.SizeCode);
        Assert.Null(dto.ImageUrl);
    }

    [Fact]
    public void ToItemDto_MapsNestedVariantDataAndPrimaryImage()
    {
        var item = new CartItem
        {
            Quantity = 2,
            UnitPrice = 10m,
            ProductVariant = new ProductVariant
            {
                Sku = "SKU-1",
                Color = new Color { Name = new LangStr("Red", "en") },
                Size = new Size { SizeCode = "L" },
                Product = new Product
                {
                    Name = new LangStr("Shirt", "en"),
                    Images = new List<ProductImage>
                    {
                        new() { Url = "second.jpg", SortOrder = 2 },
                        new() { Url = "first.jpg", SortOrder = 1 }
                    }
                }
            }
        };

        var dto = CartMapper.ToItemDto(item);

        Assert.Equal("Shirt", dto.ProductName);
        Assert.Equal("SKU-1", dto.Sku);
        Assert.Equal("Red", dto.ColorName);
        Assert.Equal("L", dto.SizeCode);
        Assert.Equal("first.jpg", dto.ImageUrl);
        Assert.Equal(20m, dto.LineTotal);
    }
}
