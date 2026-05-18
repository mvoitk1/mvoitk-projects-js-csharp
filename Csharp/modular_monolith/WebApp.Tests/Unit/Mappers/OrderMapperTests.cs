using App.BLL.Mappers;
using App.Domain;
using App.Domain.Enums;
using Xunit;

namespace WebApp.Tests.Unit.Mappers;

public class OrderMapperTests
{
    [Fact]
    public void ToDto_MapsShippingFieldsAndStatus()
    {
        var order = new Order
        {
            OrderNumber = "ORD-1",
            Status = OrderStatus.Confirmed,
            TotalAmount = 99.98m,
            ShippingFirstName = "Madis",
            ShippingCity = "Tallinn"
        };

        var dto = OrderMapper.ToDto(order);

        Assert.Equal("ORD-1", dto.OrderNumber);
        Assert.Equal("Confirmed", dto.Status);
        Assert.Equal(99.98m, dto.TotalAmount);
        Assert.Equal("Madis", dto.ShippingFirstName);
        Assert.Equal("Tallinn", dto.ShippingCity);
    }

    [Fact]
    public void ToDto_NullItems_ProducesEmptyList()
    {
        var order = new Order { OrderNumber = "ORD-2" };

        var dto = OrderMapper.ToDto(order);

        Assert.Empty(dto.Items);
    }

    [Fact]
    public void ToListItem_ItemCount_SumsItemQuantities()
    {
        var order = new Order
        {
            OrderNumber = "ORD-3",
            Status = OrderStatus.Shipped,
            Items = new List<OrderItem>
            {
                new() { Quantity = 2 },
                new() { Quantity = 3 }
            }
        };

        var dto = OrderMapper.ToListItem(order);

        Assert.Equal(5, dto.ItemCount);
        Assert.Equal("Shipped", dto.Status);
    }

    [Fact]
    public void ToItemDto_NullProductVariant_FallsBackToEmptyStrings()
    {
        var item = new OrderItem { Quantity = 1, UnitPrice = 5m, LineTotal = 5m };

        var dto = OrderMapper.ToItemDto(item);

        Assert.Equal(string.Empty, dto.ProductName);
        Assert.Equal(string.Empty, dto.Sku);
        Assert.Equal(string.Empty, dto.ColorName);
        Assert.Equal(string.Empty, dto.SizeCode);
    }

    [Fact]
    public void ToItemDto_MapsNestedVariantData()
    {
        var item = new OrderItem
        {
            Quantity = 2,
            UnitPrice = 10m,
            LineTotal = 20m,
            ProductVariant = new ProductVariant
            {
                Sku = "SKU-1",
                Color = new Color { Name = new LangStr("Red", "en") },
                Size = new Size { SizeCode = "L" },
                Product = new Product { Name = new LangStr("Shirt", "en") }
            }
        };

        var dto = OrderMapper.ToItemDto(item);

        Assert.Equal("Shirt", dto.ProductName);
        Assert.Equal("SKU-1", dto.Sku);
        Assert.Equal("Red", dto.ColorName);
        Assert.Equal("L", dto.SizeCode);
    }
}
