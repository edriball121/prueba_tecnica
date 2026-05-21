# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solution y project files primero para aprovechar el cache de layers.
# Si solo cambia código fuente (no dependencias), el restore no se repite.
COPY Polizas.slnx ./
COPY src/Polizas.Api/Polizas.Api.csproj                         src/Polizas.Api/
COPY src/Polizas.Application/Polizas.Application.csproj         src/Polizas.Application/
COPY src/Polizas.Domain/Polizas.Domain.csproj                   src/Polizas.Domain/
COPY src/Polizas.Infrastructure/Polizas.Infrastructure.csproj   src/Polizas.Infrastructure/

RUN dotnet restore src/Polizas.Api/Polizas.Api.csproj

# Copiar el resto del código fuente (sin tests ni legado)
COPY src/ src/

RUN dotnet publish src/Polizas.Api/Polizas.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Crear grupo y usuario no-root.
# Las imágenes de Microsoft ASP.NET están basadas en Debian/Ubuntu,
# por lo que se usan groupadd/useradd (no addgroup/adduser de Alpine).
RUN groupadd --system appgroup && \
    useradd --system --gid appgroup --no-create-home appuser

# Copiar artefactos publicados desde el stage de build
COPY --from=build /app/publish .

# Transferir ownership al usuario no-root antes de cambiar de usuario
RUN chown -R appuser:appgroup /app

# Nunca correr como root en producción
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "Polizas.Api.dll"]
