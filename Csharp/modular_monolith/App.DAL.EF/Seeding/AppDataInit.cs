using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public static void SeedAppData(AppDbContext context)
    {
        SeedColors(context);
        SeedSizes(context);
        SeedCategories(context);
        SeedCollections(context);
        SeedProducts(context);
    }

    private static void SeedColors(AppDbContext context)
    {
        if (context.Colors.Any()) return;

        context.Colors.AddRange(
            new Color { Name = new LangStr("Black", "en") { ["et"] = "Must" }, HexCode = "#000000" },
            new Color { Name = new LangStr("White", "en") { ["et"] = "Valge" }, HexCode = "#FFFFFF" },
            new Color { Name = new LangStr("Navy", "en") { ["et"] = "Tumesinine" }, HexCode = "#1B2A4A" }
        );
        context.SaveChanges();
    }

    private static void SeedSizes(AppDbContext context)
    {
        if (context.Sizes.Any()) return;

        context.Sizes.AddRange(
            new Size { SizeCode = "XS", DisplayName = new LangStr("Extra Small", "en") { ["et"] = "Eriti väike" } },
            new Size { SizeCode = "S",  DisplayName = new LangStr("Small", "en") { ["et"] = "Väike" } },
            new Size { SizeCode = "M",  DisplayName = new LangStr("Medium", "en") { ["et"] = "Keskmine" } },
            new Size { SizeCode = "L",  DisplayName = new LangStr("Large", "en") { ["et"] = "Suur" } },
            new Size { SizeCode = "XL", DisplayName = new LangStr("Extra Large", "en") { ["et"] = "Eriti suur" } }
        );
        context.SaveChanges();
    }

    private static void SeedCategories(AppDbContext context)
    {
        if (context.Categories.Any()) return;

        var tops = new Category { Name = new LangStr("Tops", "en") { ["et"] = "Pluusid" } };
        var bottoms = new Category { Name = new LangStr("Bottoms", "en") { ["et"] = "Püksid" } };
        var tshirts = new Category
        {
            Name = new LangStr("T-Shirts", "en") { ["et"] = "T-särgid" },
            ParentCategory = tops
        };

        context.Categories.AddRange(tops, bottoms, tshirts);
        context.SaveChanges();
    }

    private static void SeedCollections(AppDbContext context)
    {
        if (context.Collections.Any()) return;

        context.Collections.Add(new Collection
        {
            Name = new LangStr("Spring 2026", "en") { ["et"] = "Kevad 2026" },
            Description = new LangStr("Fresh looks for the new season", "en") { ["et"] = "Värske stiil uueks hooajaks" },
            LaunchDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        });
        context.SaveChanges();
    }

    private static void SeedProducts(AppDbContext context)
    {
        if (context.Products.Any()) return;

        var collection = context.Collections.First();
        var tshirtCategory = context.Categories.AsEnumerable().First(c => c.Name.ContainsKey("en") && c.Name["en"] == "T-Shirts");
        var black = context.Colors.First(c => c.HexCode == "#000000");
        var white = context.Colors.First(c => c.HexCode == "#FFFFFF");
        var sizeS = context.Sizes.First(s => s.SizeCode == "S");
        var sizeM = context.Sizes.First(s => s.SizeCode == "M");

        var product = new Product
        {
            Name = new LangStr("Classic Cotton Tee", "en") { ["et"] = "Klassikaline puuvillane t-särk" },
            Description = new LangStr("A timeless everyday essential in soft 100% cotton.", "en")
                { ["et"] = "Ajatu igapäevane põhitükk pehmes 100% puuvillases kangases." },
            Material = new LangStr("100% Cotton", "en") { ["et"] = "100% puuvill" },
            Gender = Domain.Enums.Gender.Unisex,
            IsActive = true,
            CollectionId = collection.Id,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    Url = "https://placehold.co/600x800?text=Classic+Cotton+Tee",
                    AltText = new LangStr("Classic Cotton Tee — front view", "en") { ["et"] = "Klassikaline puuvillane t-särk — eestvaade" },
                    SortOrder = 0
                }
            },
            Variants = new List<ProductVariant>
            {
                new ProductVariant { Sku = "CCT-BLK-S", Price = 29.99m, UnitPrice = 29.99m, StockQty = 20, ColorId = black.Id, SizeId = sizeS.Id },
                new ProductVariant { Sku = "CCT-BLK-M", Price = 29.99m, UnitPrice = 29.99m, StockQty = 15, ColorId = black.Id, SizeId = sizeM.Id },
                new ProductVariant { Sku = "CCT-WHT-S", Price = 29.99m, UnitPrice = 29.99m, StockQty = 18, ColorId = white.Id, SizeId = sizeS.Id },
                new ProductVariant { Sku = "CCT-WHT-M", Price = 29.99m, UnitPrice = 29.99m, StockQty = 12, ColorId = white.Id, SizeId = sizeM.Id },
            },
            ProductCategories = new List<ProductCategory>
            {
                new ProductCategory { CategoryId = tshirtCategory.Id }
            }
        };

        context.Products.Add(product);
        context.SaveChanges();
    }


    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }

    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }
}