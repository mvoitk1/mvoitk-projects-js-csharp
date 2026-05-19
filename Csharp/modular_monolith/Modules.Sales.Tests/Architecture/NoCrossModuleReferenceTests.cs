using Sales.Application;
using Sales.Infrastructure;
using Sales.Web;
using NetArchTest.Rules;

namespace Tests.Architecture;

public class NoCrossModuleReferenceTests
{
    [Fact]
    public void Sales_Application_Does_Not_Depend_On_Catalog_Or_Users()
    {
        var result = Types.InAssembly(typeof(SalesApplicationServiceCollectionExtensions).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Catalog", "Users")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Sales.Application must not depend on Catalog.* or Users.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Sales_Infrastructure_Does_Not_Depend_On_Catalog_Or_Users()
    {
        var result = Types.InAssembly(typeof(SalesDbContext).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Catalog", "Users")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Sales.Infrastructure must not depend on Catalog.* or Users.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Sales_Web_Does_Not_Depend_On_Catalog_Or_Users()
    {
        var result = Types.InAssembly(typeof(SalesWebServiceCollectionExtensions).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Catalog", "Users")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Sales.Web must not depend on Catalog.* or Users.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }
}
