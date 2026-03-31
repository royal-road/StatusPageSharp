FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/StatusPageSharp.Application/StatusPageSharp.Application.csproj src/StatusPageSharp.Application/
COPY src/StatusPageSharp.Domain/StatusPageSharp.Domain.csproj src/StatusPageSharp.Domain/
COPY src/StatusPageSharp.Infrastructure/StatusPageSharp.Infrastructure.csproj src/StatusPageSharp.Infrastructure/
COPY src/StatusPageSharp.Worker/StatusPageSharp.Worker.csproj src/StatusPageSharp.Worker/

RUN dotnet restore src/StatusPageSharp.Worker/StatusPageSharp.Worker.csproj

COPY . .

RUN dotnet publish src/StatusPageSharp.Worker/StatusPageSharp.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StatusPageSharp.Worker.dll"]
