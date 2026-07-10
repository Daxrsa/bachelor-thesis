# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + all project files first for better layer caching
COPY ECommerce.sln Directory.Build.props ./
COPY src/ECommerce.Api/ECommerce.Api.csproj                       src/ECommerce.Api/
COPY src/ECommerce.Core/ECommerce.Core.csproj                     src/ECommerce.Core/
COPY src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.PluginRuntime/ECommerce.PluginRuntime.csproj   src/ECommerce.PluginRuntime/
COPY src/ECommerce.PluginContracts/ECommerce.PluginContracts.csproj src/ECommerce.PluginContracts/
RUN dotnet restore src/ECommerce.Api/ECommerce.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/ECommerce.Api/ECommerce.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
COPY marketplace/ ./marketplace/
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
