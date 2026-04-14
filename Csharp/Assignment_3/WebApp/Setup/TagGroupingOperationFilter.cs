using System.Collections.Generic;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp.Setup;

/// <summary>
/// Renames auto-generated Swagger tags (derived from controller class names) to
/// human-readable display names.
/// </summary>
public class TagGroupingOperationFilter : IOperationFilter
{
    private static readonly Dictionary<string, string> TagMap = new()
    {
        { "Account",            "Authentication" },
        { "AdminProducts",      "Admin — Products" },
        { "AdminOrders",        "Admin — Orders" },
        { "AdminCategories",    "Admin — Categories" },
        { "AdminCollections",   "Admin — Collections" },
        { "AdminStock",         "Admin — Stock" },
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.ApiDescription.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
            || controller is null)
            return;

        if (!TagMap.TryGetValue(controller, out var friendlyTag))
            return;

        operation.Tags = new HashSet<OpenApiTagReference> { new(friendlyTag) };
    }
}
