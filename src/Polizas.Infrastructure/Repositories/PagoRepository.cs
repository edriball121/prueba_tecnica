using Microsoft.EntityFrameworkCore;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Infrastructure.Data;

namespace Polizas.Infrastructure.Repositories;

/// <summary>
/// Implementación concreta del puerto de salida <see cref="IPagoRepository"/>
/// usando EF Core con PostgreSQL a través de <see cref="PolizasDbContext"/>.
/// </summary>
public sealed class PagoRepository : IPagoRepository
{
    private readonly PolizasDbContext _context;

    /// <summary>
    /// Inicializa el repositorio con el contexto de base de datos inyectado.
    /// </summary>
    /// <param name="context">DbContext de la aplicación.</param>
    public PagoRepository(PolizasDbContext context) => _context = context;

    /// <summary>
    /// Verifica si ya existe un pago registrado con la clave de idempotencia especificada.
    /// </summary>
    /// <param name="idempotencyKey">Clave de idempotencia a verificar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns><c>true</c> si ya existe un pago con esa clave; <c>false</c> en caso contrario.</returns>
    public async Task<bool> ExisteIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        => await _context.Pagos.AnyAsync(p => p.IdempotencyKey == idempotencyKey, ct);

    /// <summary>
    /// Persiste un pago nuevo en la base de datos.
    /// Siempre agrega el pago como entidad nueva ya que los pagos no se modifican.
    /// </summary>
    /// <param name="pago">Instancia de pago a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El pago guardado con el ID asignado por la base de datos.</returns>
    public async Task<Pago> GuardarAsync(Pago pago, CancellationToken ct = default)
    {
        await _context.AddAsync(pago, ct);
        await _context.SaveChangesAsync(ct);
        return pago;
    }
}
