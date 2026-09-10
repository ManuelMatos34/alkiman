# ── Etapa 1: compilación ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para aprovechar la caché de capas de Docker
COPY src/Alkiman.API/Alkiman.API.csproj             src/Alkiman.API/
COPY src/Alkiman.Application/Alkiman.Application.csproj src/Alkiman.Application/
COPY src/Alkiman.Domain/Alkiman.Domain.csproj       src/Alkiman.Domain/
COPY src/Alkiman.Infrastructure/Alkiman.Infrastructure.csproj src/Alkiman.Infrastructure/

RUN dotnet restore src/Alkiman.API/Alkiman.API.csproj

COPY src/ src/

RUN dotnet publish src/Alkiman.API/Alkiman.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Etapa 2: imagen de producción ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Crear usuario no root para el proceso de la API
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build --chown=appuser /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Alkiman.API.dll"]
