using Catalog.Domain;
using Catalog.Domain.Enums;
using Modules.SharedKernel;

namespace Catalog.Infrastructure.Seeding;

/// <summary>
/// Seeds example catalog data (collections, categories, colors, sizes, products with
/// variants and images). Idempotent: does nothing once any product exists.
/// </summary>
public static class CatalogDataInit
{
    public static async Task SeedDataAsync(CatalogDbContext context)
    {
        if (context.Products.Any()) return;

        // --- Collections ---
        var ss26 = new Collection
        {
            Name = L("Spring / Summer 2026", "Kevad / Suvi 2026"),
            Description = L("Lightweight pieces for the warmer months.", "Kerged esemed soojemateks kuudeks."),
            LaunchDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
        };
        var essentials = new Collection
        {
            Name = L("Essentials", "Põhiriided"),
            Description = L("Everyday staples that never go out of style.", "Igapäevased riided, mis ei lähe kunagi moest."),
            IsActive = true,
        };

        // --- Categories ---
        var tShirts = new Category { Name = L("T-Shirts", "T-särgid") };
        var hoodies = new Category { Name = L("Hoodies", "Pusad") };
        var jackets = new Category { Name = L("Jackets", "Jakid") };
        var trousers = new Category { Name = L("Trousers", "Püksid") };

        // --- Colors ---
        var black = new Color { Name = L("Black", "Must"), HexCode = "#111111" };
        var white = new Color { Name = L("White", "Valge"), HexCode = "#F5F5F5" };
        var navy = new Color { Name = L("Navy", "Tumesinine"), HexCode = "#1B2A4A" };
        var olive = new Color { Name = L("Olive", "Oliiv"), HexCode = "#556B2F" };

        // --- Sizes ---
        var xs = new Size { SizeCode = "XS", DisplayName = L("Extra Small", "Eriväike") };
        var s = new Size { SizeCode = "S", DisplayName = L("Small", "Väike") };
        var m = new Size { SizeCode = "M", DisplayName = L("Medium", "Keskmine") };
        var lg = new Size { SizeCode = "L", DisplayName = L("Large", "Suur") };
        var xl = new Size { SizeCode = "XL", DisplayName = L("Extra Large", "Erisuur") };

        context.Collections.AddRange(ss26, essentials);
        context.Categories.AddRange(tShirts, hoodies, jackets, trousers);
        context.Colors.AddRange(black, white, navy, olive);
        context.Sizes.AddRange(xs, s, m, lg, xl);

        // --- Products ---
        var tee = BuildProduct(
            name: L("Classic Cotton Tee", "Klassikaline puuvillane T-särk"),
            description: L("Soft mid-weight cotton crew-neck tee with a relaxed fit.",
                "Pehme keskmise paksusega puuvillane ümarkaelusega T-särk vabas lõikes."),
            material: L("100% organic cotton", "100% mahepuuvill"),
            gender: Gender.Unisex,
            collection: essentials,
            categories: [tShirts],
            imageSeed: "cotton-tee",
            skuPrefix: "TEE",
            price: 24.90m,
            colors: [black, white, navy],
            sizes: [s, m, lg, xl]);

        var hoodie = BuildProduct(
            name: L("Heavyweight Hoodie", "Raske pusa"),
            description: L("Brushed-back fleece hoodie with a double-layer hood.",
                "Pehme sisepinnaga fliispusa kahekihilise kapuutsiga."),
            material: L("80% cotton, 20% polyester", "80% puuvill, 20% polüester"),
            gender: Gender.Unisex,
            collection: essentials,
            categories: [hoodies],
            imageSeed: "hoodie",
            skuPrefix: "HOOD",
            price: 59.90m,
            colors: [black, olive],
            sizes: [s, m, lg, xl]);

        var jacket = BuildProduct(
            name: L("Field Jacket", "Välijakk"),
            description: L("Water-repellent cotton field jacket with four utility pockets.",
                "Vett hülgav puuvillane välijakk nelja taskuga."),
            material: L("100% waxed cotton", "100% vahatatud puuvill"),
            gender: Gender.Men,
            collection: ss26,
            categories: [jackets],
            imageSeed: "field-jacket",
            skuPrefix: "JKT",
            price: 129.00m,
            colors: [navy, olive],
            sizes: [m, lg, xl]);

        var chinos = BuildProduct(
            name: L("Tailored Chinos", "Vabaaja püksid"),
            description: L("Slim-tapered stretch chinos for all-day comfort.",
                "Kitseneva lõikega elastsed chino-püksid mugavaks kandmiseks."),
            material: L("98% cotton, 2% elastane", "98% puuvill, 2% elastaan"),
            gender: Gender.Women,
            collection: ss26,
            categories: [trousers],
            imageSeed: "chinos",
            skuPrefix: "CHN",
            price: 74.50m,
            colors: [black, navy],
            sizes: [xs, s, m, lg]);

        context.Products.AddRange(tee, hoodie, jacket, chinos);

        await context.SaveChangesAsync();
    }

    private static Product BuildProduct(
        LangStr name, LangStr description, LangStr material, Gender gender,
        Collection collection, Category[] categories, string imageSeed,
        string skuPrefix, decimal price, Color[] colors, Size[] sizes)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Material = material,
            Gender = gender,
            Collection = collection,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Variants = new List<ProductVariant>(),
            Images = new List<ProductImage>(),
            ProductCategories = new List<ProductCategory>(),
        };

        foreach (var category in categories)
            product.ProductCategories.Add(new ProductCategory { Category = category });

        product.Images.Add(new ProductImage
        {
            Url = $"https://picsum.photos/seed/{imageSeed}/600/800",
            AltText = name,
            SortOrder = 0,
        });
        product.Images.Add(new ProductImage
        {
            Url = $"https://picsum.photos/seed/{imageSeed}-2/600/800",
            AltText = name,
            SortOrder = 1,
        });

        var stock = 12;
        foreach (var color in colors)
        {
            foreach (var size in sizes)
            {
                product.Variants.Add(new ProductVariant
                {
                    Sku = $"{skuPrefix}-{color.HexCode.TrimStart('#')[..3]}-{size.SizeCode}",
                    Price = price,
                    UnitPrice = price,
                    StockQty = stock,
                    IsActive = true,
                    Color = color,
                    Size = size,
                });
                stock = stock == 0 ? 12 : stock - 2; // vary stock, include a sold-out variant
            }
        }

        return product;
    }

    private static LangStr L(string en, string et)
    {
        var langStr = new LangStr(en, "en");
        langStr.SetTranslation(et, "et");
        return langStr;
    }
}
