# syntax=docker/dockerfile:1

# ---- build: publishes the ASP.NET Core API together with the Blazor WebAssembly client ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached unless a project file changes).
COPY ShopDemo.Domain/ShopDemo.Domain.csproj ShopDemo.Domain/
COPY ShopDemo.Contracts/ShopDemo.Contracts.csproj ShopDemo.Contracts/
COPY ShopDemo.Infrastructure/ShopDemo.Infrastructure.csproj ShopDemo.Infrastructure/
COPY ShopDemo.Client/ShopDemo.Client.csproj ShopDemo.Client/
COPY ShopDemo.Server/ShopDemo.Server.csproj ShopDemo.Server/
RUN dotnet restore ShopDemo.Server/ShopDemo.Server.csproj

COPY ShopDemo.Domain/ ShopDemo.Domain/
COPY ShopDemo.Contracts/ ShopDemo.Contracts/
COPY ShopDemo.Infrastructure/ ShopDemo.Infrastructure/
COPY ShopDemo.Client/ ShopDemo.Client/
COPY ShopDemo.Server/ ShopDemo.Server/
RUN dotnet publish ShopDemo.Server/ShopDemo.Server.csproj -c Release --no-restore -o /app

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ShopDemo.Server.dll"]
