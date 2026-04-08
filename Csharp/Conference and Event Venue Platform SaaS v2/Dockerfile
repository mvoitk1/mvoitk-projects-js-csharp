FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["VenuePlatform.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["WebApp/WebApp.csproj", "WebApp/"]
COPY ["App.BLL/App.BLL.csproj", "App.BLL/"]
COPY ["App.DAL.EF/App.DAL.EF.csproj", "App.DAL.EF/"]
COPY ["App.Domain/App.Domain.csproj", "App.Domain/"]
COPY ["App.DTO/App.DTO.csproj", "App.DTO/"]
COPY ["App.Helpers/App.Helpers.csproj", "App.Helpers/"]
COPY ["App.Resources/App.Resources.csproj", "App.Resources/"]
COPY ["Base.Resources/Base.Resources.csproj", "Base.Resources/"]
COPY ["WebApp.Tests/WebApp.Tests.csproj", "WebApp.Tests/"]

RUN dotnet restore "VenuePlatform.sln"

COPY . .

# Remove Windows-style generated artifact directories that were committed with
# literal backslashes in their names. These break Linux container builds.
RUN find . -type d \( -name 'bin\\*' -o -name 'obj\\*' \) -prune -exec rm -rf '{}' +

RUN dotnet publish "WebApp/WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebApp.dll"]
