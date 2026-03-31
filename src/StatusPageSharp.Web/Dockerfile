FROM node:25-alpine AS assets
WORKDIR /src/StatusPageSharp.Web

COPY src/StatusPageSharp.Web/package.json src/StatusPageSharp.Web/package-lock.json ./
RUN npm ci --prefer-offline

COPY src/StatusPageSharp.Web/Styles ./Styles
RUN npm run css:build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/StatusPageSharp.Application/StatusPageSharp.Application.csproj src/StatusPageSharp.Application/
COPY src/StatusPageSharp.Domain/StatusPageSharp.Domain.csproj src/StatusPageSharp.Domain/
COPY src/StatusPageSharp.Infrastructure/StatusPageSharp.Infrastructure.csproj src/StatusPageSharp.Infrastructure/
COPY src/StatusPageSharp.Web/StatusPageSharp.Web.csproj src/StatusPageSharp.Web/

RUN dotnet restore src/StatusPageSharp.Web/StatusPageSharp.Web.csproj

COPY . .
COPY --from=assets /src/StatusPageSharp.Web/wwwroot/css/site.css /src/src/StatusPageSharp.Web/wwwroot/css/site.css

RUN dotnet publish src/StatusPageSharp.Web/StatusPageSharp.Web.csproj -c Release -o /app/publish /p:EnableTailwindBuild=false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "StatusPageSharp.Web.dll"]
