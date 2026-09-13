# ==============================================================================
# Dockerfile para despliegue de WARA.Backend en Render / Cloud Platforms
# .NET 8.0 ASP.NET Core Web API
# ==============================================================================

# Etapa 1: Compilación y restauración
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solución y csproj para optimizar cache de capas en Docker
COPY ["WARA.sln", "./"]
COPY ["src/WARA.Domain/WARA.Domain.csproj", "src/WARA.Domain/"]
COPY ["src/WARA.Application/WARA.Application.csproj", "src/WARA.Application/"]
COPY ["src/WARA.Infrastructure/WARA.Infrastructure.csproj", "src/WARA.Infrastructure/"]
COPY ["src/WARA.API/WARA.API.csproj", "src/WARA.API/"]
COPY ["tests/WARA.Tests/WARA.Tests.csproj", "tests/WARA.Tests/"]

# Restaurar dependencias
RUN dotnet restore "WARA.sln"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar la API
WORKDIR "/src/src/WARA.API"
RUN dotnet publish "WARA.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Entorno de ejecución ligero
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Configuración de puerto por defecto (Render inyectará $PORT automáticamente si aplica)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Copiar los artefactos compilados desde la etapa de build
COPY --from=build /app/publish .

# Comando de inicio
ENTRYPOINT ["dotnet", "WARA.API.dll"]
