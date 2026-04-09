using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var apiVersionDescription in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                apiVersionDescription.GroupName,
                new OpenApiInfo()
                {
                    Title = $"API {apiVersionDescription.ApiVersion}",
                    Version = apiVersionDescription.ApiVersion.ToString(),
                }
            );
        }

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPathAndFile = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPathAndFile);


        // use FullName for schemaId - avoids conflicts between classes using the same name (which are in different namespaces)
        options.CustomSchemaIds(i => i.FullName);


        // add support for authentication
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Description =
                "JWT Authorization header using the Bearer scheme.\r\n<br>" +
                "Enter 'Bearer'[space] and then your token in the text box below.\r\n<br>" +
                "Example: <b>Bearer eyJhbGciOiJIUzUxMiIsIn...</b>\r\n<br>" +
                "You will get the bearer from the <i>account/login</i> or <i>account/register</i> endpoint.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        options.DocumentFilter<BearerSecurityRequirementDocumentFilter>();
    }
}

public class BearerSecurityRequirementDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", swaggerDoc)] = new List<string>()
            }
        };
    }
}