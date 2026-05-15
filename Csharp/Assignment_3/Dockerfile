FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["webapp2025s.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["WebApp/WebApp.csproj", "WebApp/"]
COPY ["App.BLL/App.BLL.csproj", "App.BLL/"]
COPY ["App.BLL.Contracts/App.BLL.Contracts.csproj", "App.BLL.Contracts/"]
COPY ["App.BLL.DTO/App.BLL.DTO.csproj", "App.BLL.DTO/"]
COPY ["App.DAL.EF/App.DAL.EF.csproj", "App.DAL.EF/"]
COPY ["App.DAL.Contracts/App.DAL.Contracts.csproj", "App.DAL.Contracts/"]
COPY ["App.DAL.DTO/App.DAL.DTO.csproj", "App.DAL.DTO/"]
COPY ["App.Domain/App.Domain.csproj", "App.Domain/"]
COPY ["App.DTO/App.DTO.csproj", "App.DTO/"]
COPY ["App.Resources/App.Resources.csproj", "App.Resources/"]
COPY ["Base.BLL/Base.BLL.csproj", "Base.BLL/"]
COPY ["Base.BLL.Contracts/Base.BLL.Contracts.csproj", "Base.BLL.Contracts/"]
COPY ["Base.DAL.EF/Base.DAL.EF.csproj", "Base.DAL.EF/"]
COPY ["Base.DAL.Contracts/Base.DAL.Contracts.csproj", "Base.DAL.Contracts/"]
COPY ["Base.Domain/Base.Domain.csproj", "Base.Domain/"]
COPY ["Base.Contracts/Base.Contracts.csproj", "Base.Contracts/"]
COPY ["Base.Helpers/Base.Helpers.csproj", "Base.Helpers/"]
COPY ["Base.Resources/Base.Resources.csproj", "Base.Resources/"]
COPY ["WebApp.Tests/WebApp.Tests.csproj", "WebApp.Tests/"]

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
