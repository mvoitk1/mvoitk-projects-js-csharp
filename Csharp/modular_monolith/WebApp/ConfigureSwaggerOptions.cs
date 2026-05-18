using System.IO;
using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp.Setup;

namespace WebApp;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _descriptionProvider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider descriptionProvider)
    {
        _descriptionProvider = descriptionProvider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _descriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "Brand E-Commerce API",
                    Version = description.ApiVersion.ToString(),
                    Description =
                        "REST API for the brand fashion e-commerce platform.\n\n" +
                        "**Authentication:** Use `POST /api/v1/Account/Login` to obtain a JWT, " +
                        "then click **Authorize** and enter `Bearer <your-token>`.\n\n" +
                        "**Roles:** Endpoints under `/admin/` require the `Admin` role."
                }
            );
        }

        // Strip the "App.DTO.v1." namespace prefix so schema names are readable
        // e.g. "App.DTO.v1.Products.ProductDto" → "Products.ProductDto"
        options.CustomSchemaIds(t =>
        {
            var name = t.FullName ?? t.Name;
            const string prefix = "App.DTO.v1.";
            if (name.StartsWith(prefix))
                name = name[prefix.Length..];
            return name.Replace("+", ".");
        });

        // Include XML doc comments from this assembly
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);

        // Define the Bearer JWT security scheme
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header. Enter your token below.\n\n" +
                "Example: `Bearer eyJhbGci...`",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        // Apply security + 401/403 only to [Authorize] endpoints
        options.OperationFilter<AuthOperationFilter>();

        // Assign human-readable tags at discovery time (avoids empty ghost groups)
        options.TagActionsBy(api =>
        {
            if (!api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
                || controller is null)
                return new[] { "Other" };

            return controller switch
            {
                "Account"            => new[] { "Authentication" },
                "AdminProducts"      => new[] { "Admin — Products" },
                "AdminOrders"        => new[] { "Admin — Orders" },
                "AdminCategories"    => new[] { "Admin — Categories" },
                "AdminCollections"   => new[] { "Admin — Collections" },
                "AdminStock"         => new[] { "Admin — Stock" },
                _                    => new[] { controller }
            };
        });
    }
}
