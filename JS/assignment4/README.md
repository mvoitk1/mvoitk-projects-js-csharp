# taltech.akaver.com

Simple API for js course

## Useful commands in .net console CLI

Install tooling

~~~bash
dotnet tool update -g dotnet-ef
dotnet tool update -g dotnet-aspnet-codegenerator 
~~~

EF Core migrations

~~~bash
dotnet ef migrations --project App.DAL.EF --startup-project WebApp add FOOBAR
dotnet ef database   --project App.DAL.EF --startup-project WebApp update
dotnet ef database   --project App.DAL.EF --startup-project WebApp drop
~~~

Install package  
Microsoft.VisualStudio.Web.CodeGeneration.Design  
to WebApp

Scaffold MVC Web controllers (run from WebApp )
~~~
dotnet aspnet-codegenerator controller -name TodoCategoriesController   -actions -m  App.Domain.Todo.TodoCategory  -dc App.DAL.EF.AppDbContext -outDir Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f
dotnet aspnet-codegenerator controller -name TodoPrioritiesController   -actions -m  App.Domain.Todo.TodoPriority  -dc App.DAL.EF.AppDbContext -outDir Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f
dotnet aspnet-codegenerator controller -name TodoTasksController        -actions -m  App.Domain.Todo.TodoTask      -dc App.DAL.EF.AppDbContext -outDir Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f
dotnet aspnet-codegenerator controller -name ListItemsController        -actions -m  App.Domain.ApiKey.SimpleList.ListItem      -dc App.DAL.EF.AppDbContext -outDir Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f
dotnet aspnet-codegenerator controller -name ApiKeysController        -actions -m  App.Domain.ApiKey.ApiKey      -dc App.DAL.EF.AppDbContext -outDir Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f

dotnet aspnet-codegenerator controller -name RefreshTokensController   -actions -m  App.Domain.Identity.RefreshToken  -dc App.DAL.EF.AppDbContext -outDir Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f

~~~

Scaffold Api controllers
~~~
dotnet aspnet-codegenerator controller -name TodoCategoriesController  -m App.Domain.Todo.TodoCategory     -actions -dc AppDbContext -outDir ApiControllers -api --useAsyncActions  -f
dotnet aspnet-codegenerator controller -name TodoPrioritiesController  -m App.Domain.Todo.TodoPriority     -actions -dc AppDbContext -outDir ApiControllers -api --useAsyncActions  -f
dotnet aspnet-codegenerator controller -name TodoTasksController       -m App.Domain.Todo.TodoTask         -actions -dc AppDbContext -outDir ApiControllers -api --useAsyncActions  -f
dotnet aspnet-codegenerator controller -name ListItemsController       -m App.Domain.ApiKey.SimpleList.ListItem         -actions -dc AppDbContext -outDir ApiControllers -api --useAsyncActions  -f
~~~

Scaffold the identity pages
~~~bash
cd WebApp
dotnet aspnet-codegenerator identity -dc App.DAL.EF.AppDbContext -f
~~~