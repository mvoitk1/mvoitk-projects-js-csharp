# https://hub.docker.com/_/microsoft-dotnet-sdk/
# Debian 10 based
# FROM mcr.microsoft.com/dotnet/sdk:latest AS build
# Ubuntu 20.04
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# install nodejs & npm
#RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
#RUN apt-get install -y nodejs apt-utils
#RUN node --version
#RUN npm install -g npm
#RUN npm --version


WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Directory.Build.props .
COPY nuget.config .

# project files for restore
COPY com.akaver.Contracts.Domain.Base/*.csproj ./com.akaver.Contracts.Domain.Base/
COPY com.akaver.DAL.Base.EF/*.csproj ./com.akaver.DAL.Base.EF/
COPY com.akaver.Domain.Base/*.csproj ./com.akaver.Domain.Base/
COPY com.akaver.Extensions/*.csproj ./com.akaver.Extensions/

COPY App.DAL.EF/*.csproj ./App.DAL.EF/
COPY App.Domain/*.csproj ./App.Domain/
COPY PublicApi.DTO.v1/*.csproj ./PublicApi.DTO.v1/
COPY Resources/*.csproj ./Resources/
COPY WebApp/*.csproj ./WebApp/
COPY Tests/*.csproj ./Tests/

RUN dotnet restore


# copy everything else and build app

COPY com.akaver.Contracts.Domain.Base/. ./com.akaver.Contracts.Domain.Base/
COPY com.akaver.DAL.Base.EF/. ./com.akaver.DAL.Base.EF/
COPY com.akaver.Domain.Base/. ./com.akaver.Domain.Base/
COPY com.akaver.Extensions/. ./com.akaver.Extensions/

COPY App.DAL.EF/. ./App.DAL.EF/
COPY App.Domain/. ./App.Domain/
COPY PublicApi.DTO.v1/. ./PublicApi.DTO.v1/
COPY Resources/. ./Resources/
COPY WebApp/. ./WebApp/
COPY Tests/. ./Tests/

# build js/css stuff
#COPY SiteJS/. ./SiteJS/
#WORKDIR /source/SiteJS
#RUN npm install -g npm@latest
#RUN npm install
#RUN npm run build

# build and publish WebApp
WORKDIR /source/WebApp
RUN dotnet publish -c release -o /app --no-restore


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0

EXPOSE 8080

# set timezone
# https://serverfault.com/questions/683605/docker-container-time-timezone-will-not-reflect-changes
ENV TZ 'Europe/Tallinn'
RUN echo $TZ > /etc/timezone && \
    apt-get update && apt-get install -y tzdata && \
    rm /etc/localtime && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    dpkg-reconfigure -f noninteractive tzdata && \
    apt-get clean

WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "WebApp.dll"]
