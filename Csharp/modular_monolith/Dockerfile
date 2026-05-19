FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["webapp2025s.sln", "./"]
COPY ["Directory.Build.props", "./"]

# Host
COPY ["WebApp/WebApp.csproj", "WebApp/"]

# Shared base layer
COPY ["App.Resources/App.Resources.csproj", "App.Resources/"]
COPY ["Base.BLL/Base.BLL.csproj", "Base.BLL/"]
COPY ["Base.BLL.Contracts/Base.BLL.Contracts.csproj", "Base.BLL.Contracts/"]
COPY ["Base.Contracts/Base.Contracts.csproj", "Base.Contracts/"]
COPY ["Base.DAL.Contracts/Base.DAL.Contracts.csproj", "Base.DAL.Contracts/"]
COPY ["Base.DAL.EF/Base.DAL.EF.csproj", "Base.DAL.EF/"]
COPY ["Base.Domain/Base.Domain.csproj", "Base.Domain/"]
COPY ["Base.Helpers/Base.Helpers.csproj", "Base.Helpers/"]
COPY ["Base.Resources/Base.Resources.csproj", "Base.Resources/"]

# Module composition contracts
COPY ["Modules.Abstractions/Modules.Abstractions.csproj", "Modules.Abstractions/"]
COPY ["Modules.Contracts/Modules.Contracts.csproj", "Modules.Contracts/"]
COPY ["Modules.SharedKernel/Modules.SharedKernel.csproj", "Modules.SharedKernel/"]

# Catalog module
COPY ["Catalog.Domain/Catalog.Domain.csproj", "Catalog.Domain/"]
COPY ["Catalog.Application/Catalog.Application.csproj", "Catalog.Application/"]
COPY ["Catalog.Infrastructure/Catalog.Infrastructure.csproj", "Catalog.Infrastructure/"]
COPY ["Catalog.Web/Catalog.Web.csproj", "Catalog.Web/"]
COPY ["Catalog.Module/Catalog.Module.csproj", "Catalog.Module/"]

# Sales module
COPY ["Sales.Domain/Sales.Domain.csproj", "Sales.Domain/"]
COPY ["Sales.Application/Sales.Application.csproj", "Sales.Application/"]
COPY ["Sales.Infrastructure/Sales.Infrastructure.csproj", "Sales.Infrastructure/"]
COPY ["Sales.Web/Sales.Web.csproj", "Sales.Web/"]
COPY ["Sales.Module/Sales.Module.csproj", "Sales.Module/"]

# Users module
COPY ["Users.Domain/Users.Domain.csproj", "Users.Domain/"]
COPY ["Users.Application/Users.Application.csproj", "Users.Application/"]
COPY ["Users.Infrastructure/Users.Infrastructure.csproj", "Users.Infrastructure/"]
COPY ["Users.Web/Users.Web.csproj", "Users.Web/"]
COPY ["Users.Module/Users.Module.csproj", "Users.Module/"]

# Tests
COPY ["Tests.Shared/Tests.Shared.csproj", "Tests.Shared/"]
COPY ["Modules.Catalog.Tests/Modules.Catalog.Tests.csproj", "Modules.Catalog.Tests/"]
COPY ["Modules.Sales.Tests/Modules.Sales.Tests.csproj", "Modules.Sales.Tests/"]
COPY ["Modules.Users.Tests/Modules.Users.Tests.csproj", "Modules.Users.Tests/"]
COPY ["WebApp.Tests/WebApp.Tests.csproj", "WebApp.Tests/"]
COPY ["WebApp.Tests.Postgres/WebApp.Tests.Postgres.csproj", "WebApp.Tests.Postgres/"]

RUN dotnet restore "webapp2025s.sln"

COPY . .

# Remove Windows-style generated artifact directories that were committed with
# literal backslashes in their names. These break Linux container builds.
RUN find . -type d \( -name 'bin\\*' -o -name 'obj\\*' \) -prune -exec rm -rf '{}' +

# Restore client-side libraries (Bootstrap, jQuery, Alpine.js, htmx, etc.)
RUN dotnet tool install --global Microsoft.Web.LibraryManager.Cli
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN cd WebApp && libman restore

RUN dotnet publish "WebApp/WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebApp.dll"]
