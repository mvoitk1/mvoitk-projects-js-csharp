using Users.Application;
using Users.Infrastructure;
using Users.Web;
using NetArchTest.Rules;

namespace Tests.Architecture;

public class NoCrossModuleReferenceTests
{
    [Fact]
    public void Users_Application_Does_Not_Depend_On_Catalog_Or_Sales()
    {
        var result = Types.InAssembly(typeof(UsersApplicationServiceCollectionExtensions).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Catalog", "Sales")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Users.Application must not depend on Catalog.* or Sales.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Users_Infrastructure_Does_Not_Depend_On_Catalog_Or_Sales()
    {
        var result = Types.InAssembly(typeof(UsersDbContext).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Catalog", "Sales")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Users.Infrastructure must not depend on Catalog.* or Sales.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Users_Web_Does_Not_Depend_On_Catalog_Or_Sales()
    {
        var result = Types.InAssembly(typeof(UsersWebServiceCollectionExtensions).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Catalog", "Sales")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Users.Web must not depend on Catalog.* or Sales.* namespaces. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }
}
