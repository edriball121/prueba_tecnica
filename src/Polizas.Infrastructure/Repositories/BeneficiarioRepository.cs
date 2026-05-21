using Microsoft.EntityFrameworkCore;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Infrastructure.Data;

namespace Polizas.Infrastructure.Repositories;

/// <summary>
/// Implementación concreta del puerto de salida <see cref="IBeneficiarioRepository"/>
/// usando EF Core con PostgreSQL a través de <see cref="PolizasDbContext"/>.
/// </summary>
public sealed class BeneficiarioRepository : IBeneficiarioRepository
{
    private readonly PolizasDbContext _context;

    /// <summary>
    /// Inicializa el repositorio con el contexto de base de datos inyectado.
    /// </summary>
    /// <param name="context">DbContext de la aplicación.</param>
    public BeneficiarioRepository(PolizasDbContext context) => _context = context;

    /// <summary>
    /// Obtiene un beneficiario por su número de documento de identidad.
    /// </summary>
    /// <param name="documento">Documento de identidad a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El beneficiario encontrado, o <c>null</c> si no existe.</returns>
    public async Task<Beneficiario?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct = default)
        => await _context.Beneficiarios.FirstOrDefaultAsync(b => b.Documento == documento, ct);

    /// <summary>
    /// Persiste un beneficiario nuevo o actualizado en la base de datos.
    /// Si el beneficiario es nuevo (Id == 0), se agrega al contexto; de lo contrario,
    /// EF Core detecta los cambios automáticamente por tracking.
    /// </summary>
    /// <param name="beneficiario">Instancia de beneficiario a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El beneficiario guardado con el ID asignado por la base de datos.</returns>
    public async Task<Beneficiario> GuardarAsync(Beneficiario beneficiario, CancellationToken ct = default)
    {
        if (beneficiario.Id == 0)
            await _context.AddAsync(beneficiario, ct);
        // Si ya tiene ID, EF Core lo tiene tracked desde la consulta anterior

        await _context.SaveChangesAsync(ct);
        return beneficiario;
    }
}
