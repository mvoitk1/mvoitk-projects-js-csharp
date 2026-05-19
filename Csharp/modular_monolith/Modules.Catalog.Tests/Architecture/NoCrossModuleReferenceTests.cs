using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Web;
using NetArchTest.Rules;

namespace Tests.Architecture;

public class NoCrossModuleReferenceTests
{
    [Fact]
    public void Catalog_Application_Does_Not_Depend_On_Sales_Or_Users()
    {
        var result = Types.InAssembly(typeof(CatalogApplicationServiceCollectionExtensions).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Sales", "Users")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Catalog.Application must not depend on Sales.* or Users.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Catalog_Infrastructure_Does_Not_Depend_On_Sales_Or_Users()
    {
        var result = Types.InAssembly(typeof(CatalogDbContext).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Sales", "Users")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Catalog.Infrastructure must not depend on Sales.* or Users.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Catalog_Web_Does_Not_Depend_On_Sales_Or_Users()
    {
        var result = Types.InAssembly(typeof(CatalogWebServiceCollectionExtensions).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Sales", "Users")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Catalog.Web must not depend on Sales.* or Users.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }
}
