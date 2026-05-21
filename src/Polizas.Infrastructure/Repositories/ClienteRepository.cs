using Microsoft.EntityFrameworkCore;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Infrastructure.Data;

namespace Polizas.Infrastructure.Repositories;

/// <summary>
/// Implementación concreta del puerto de salida <see cref="IClienteRepository"/>
/// usando EF Core con PostgreSQL a través de <see cref="PolizasDbContext"/>.
/// </summary>
public sealed class ClienteRepository : IClienteRepository
{
    private readonly PolizasDbContext _context;

    /// <summary>
    /// Inicializa el repositorio con el contexto de base de datos inyectado.
    /// </summary>
    /// <param name="context">DbContext de la aplicación.</param>
    public ClienteRepository(PolizasDbContext context) => _context = context;

    /// <summary>
    /// Obtiene un cliente por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente encontrado, o <c>null</c> si no existe.</returns>
    public async Task<Cliente?> ObtenerPorIdAsync(long id, CancellationToken ct = default)
        => await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>
    /// Obtiene un cliente por su número de documento de identidad.
    /// </summary>
    /// <param name="documento">Documento de identidad a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente encontrado, o <c>null</c> si no existe.</returns>
    public async Task<Cliente?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct = default)
        => await _context.Clientes.FirstOrDefaultAsync(c => c.Documento == documento, ct);

    /// <summary>
    /// Persiste un cliente nuevo o actualizado en la base de datos.
    /// Si el cliente es nuevo (Id == 0), se agrega al contexto; de lo contrario,
    /// EF Core detecta los cambios automáticamente por tracking.
    /// </summary>
    /// <param name="cliente">Instancia de cliente a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente guardado con el ID asignado por la base de datos.</returns>
    public async Task<Cliente> GuardarAsync(Cliente cliente, CancellationToken ct = default)
    {
        if (cliente.Id == 0)
            await _context.AddAsync(cliente, ct);
        // Si ya tiene ID, EF Core lo tiene tracked desde la consulta anterior

        await _context.SaveChangesAsync(ct);
        return cliente;
    }
}
