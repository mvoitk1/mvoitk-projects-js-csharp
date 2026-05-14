using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;

namespace WebApp.Tests.Helpers;

/// <summary>The ids of the entities created by <see cref="TestData.SeedCatalog"/>.</summary>
public class SeededCatalog
{
    public Guid ColorId { get; init; }
    public Guid SizeId { get; init; }
    public Guid CategoryMenId { get; init; }
    public Guid CategoryWomenId { get; init; }
    public Guid CollectionAId { get; init; }
    public Guid CollectionBId { get; init; }

    /// <summary>Active, Gender.Men, in CollectionA, in CategoryMen, one variant (stock 10), one image.</summary>
    public Guid ActiveProductId { get; init; }
    public Guid ActiveVariantId { get; init; }

    /// <summary>Active, Gender.Women, in CollectionB, in CategoryWomen, one variant (stock 5).</summary>
    public Guid WomenProductId { get; init; }
    public Guid WomenVariantId { get; init; }

    /// <summary>Inactive product — must never appear in public listings.</summary>
    public Guid InactiveProductId { get; init; }
}

/// <summary>Deterministic catalogue seeding shared by service and repository unit tests.</summary>
public static class TestData
{
    public static SeededCatalog SeedCatalog(AppDbContext ctx)
    {
        var color = new Color { Name = new LangStr("Black", "en") { ["et"] = "Must" }, HexCode = "#000000" };
        var size = new Size { SizeCode = "M", DisplayName = new LangStr("Medium", "en") { ["et"] = "Keskmine" } };

        var categoryMen = new Category { Name = new LangStr("Men", "en") { ["et"] = "Mehed" } };
        var categoryWomen = new Category { Name = new LangStr("Women", "en") { ["et"] = "Naised" } };

        var collectionA = new Collection
        {
            Name = new LangStr("Spring", "en") { ["et"] = "Kevad" },
            Description = new LangStr("Spring line", "en") { ["et"] = "Kevadkollektsioon" },
            LaunchDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };
        var collectionB = new Collection
        {
            Name = new LangStr("Autumn", "en") { ["et"] = "Sügis" },
            Description = new LangStr("Autumn line", "en") { ["et"] = "Sügiskollektsioon" },
            LaunchDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        var activeProduct = new Product
        {
            Name = new LangStr("Classic Tee", "en") { ["et"] = "Klassikaline särk" },
            Description = new LangStr("A classic tee", "en") { ["et"] = "Klassikaline särk" },
            Material = new LangStr("Cotton", "en") { ["et"] = "Puuvill" },
            Gender = Gender.Men,
            IsActive = true,
            Collection = collectionA
        };
        var activeVariant = new ProductVariant
        {
            Sku = "TEE-BLK-M",
            Price = 29.99m,
            UnitPrice = 12.00m,
            StockQty = 10,
            IsActive = true,
            Color = color,
            Size = size,
            Product = activeProduct
        };
        var activeImage = new ProductImage
        {
            Url = "https://example.test/tee.jpg",
            AltText = new LangStr("Classic Tee photo", "en") { ["et"] = "Klassikalise särgi foto" },
            SortOrder = 0,
            Product = activeProduct
        };

        var womenProduct = new Product
        {
            Name = new LangStr("Summer Dress", "en") { ["et"] = "Suvekleit" },
            Description = new LangStr("A summer dress", "en") { ["et"] = "Suvekleit" },
            Material = new LangStr("Linen", "en") { ["et"] = "Lina" },
            Gender = Gender.Women,
            IsActive = true,
            Collection = collectionB
        };
        var womenVariant = new ProductVariant
        {
            Sku = "DRESS-BLK-M",
            Price = 59.99m,
            UnitPrice = 25.00m,
            StockQty = 5,
            IsActive = true,
            Color = color,
            Size = size,
            Product = womenProduct
        };

        var inactiveProduct = new Product
        {
            Name = new LangStr("Discontinued Hoodie", "en") { ["et"] = "Lõpetatud pusa" },
            Description = new LangStr("Gone", "en"),
            Material = new LangStr("Fleece", "en"),
            Gender = Gender.Unisex,
            IsActive = false,
            Collection = collectionA
        };

        ctx.Colors.Add(color);
        ctx.Sizes.Add(size);
        ctx.Categories.AddRange(categoryMen, categoryWomen);
        ctx.Collections.AddRange(collectionA, collectionB);
        ctx.Products.AddRange(activeProduct, womenProduct, inactiveProduct);
        ctx.ProductVariants.AddRange(activeVariant, womenVariant);
        ctx.ProductImages.Add(activeImage);
        ctx.ProductCategories.AddRange(
            new ProductCategory { Product = activeProduct, Category = categoryMen },
            new ProductCategory { Product = womenProduct, Category = categoryWomen });

        ctx.SaveChanges();

        return new SeededCatalog
        {
            ColorId = color.Id,
            SizeId = size.Id,
            CategoryMenId = categoryMen.Id,
            CategoryWomenId = categoryWomen.Id,
            CollectionAId = collectionA.Id,
            CollectionBId = collectionB.Id,
            ActiveProductId = activeProduct.Id,
            ActiveVariantId = activeVariant.Id,
            WomenProductId = womenProduct.Id,
            WomenVariantId = womenVariant.Id,
            InactiveProductId = inactiveProduct.Id
        };
    }

    /// <summary>Create an active cart with a single item for the given user and variant.</summary>
    public static Cart SeedCartWithItem(AppDbContext ctx, Guid userId, Guid variantId, int quantity, decimal unitPrice)
    {
        var cart = new Cart
        {
            AppUserId = userId,
            Status = CartStatus.Active,
            Items = new List<CartItem>
            {
                new() { ProductVariantId = variantId, Quantity = quantity, UnitPrice = unitPrice }
            }
        };
        ctx.Carts.Add(cart);
        ctx.SaveChanges();
        return cart;
    }
}
