using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polizas.Application.Interfaces;
using Polizas.Infrastructure.Data;
using Polizas.Infrastructure.Repositories;

namespace Polizas.Infrastructure.Extensions;

/// <summary>
/// Métodos de extensión para registrar todos los servicios de la capa Infrastructure
/// en el contenedor de inyección de dependencias de ASP.NET Core.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Registra todos los servicios de la capa Infrastructure:
    /// DbContext (PostgreSQL), repositorios y configuración de opciones.
    /// </summary>
    /// <param name="services">Colección de servicios del contenedor DI.</param>
    /// <param name="configuration">Configuración de la aplicación (appsettings.json, variables de entorno, etc.).</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. DbContext con Npgsql (PostgreSQL)
        services.AddDbContext<PolizasDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsAssembly("Polizas.Infrastructure")));

        // 2. Repositorios (adapters de salida / driven adapters)
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IPolizaRepository, PolizaRepository>();
        services.AddScoped<IBeneficiarioRepository, BeneficiarioRepository>();
        services.AddScoped<IPagoRepository, PagoRepository>();

        return services;
    }
}
