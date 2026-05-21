using Polizas.Domain.Entities;

namespace Polizas.Application.Interfaces;

/// <summary>
/// Puerto de salida (driven port) que define las operaciones de persistencia
/// para la entidad <see cref="Cliente"/>.
/// La implementación concreta vive en la capa de Infrastructure.
/// </summary>
public interface IClienteRepository
{
    /// <summary>
    /// Obtiene un cliente por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del cliente a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente encontrado, o <c>null</c> si no existe.</returns>
    Task<Cliente?> ObtenerPorIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Obtiene un cliente por su número de documento de identidad.
    /// </summary>
    /// <param name="documento">Documento de identidad a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente encontrado, o <c>null</c> si no existe.</returns>
    Task<Cliente?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct = default);

    /// <summary>
    /// Persiste un cliente nuevo o actualizado en el almacenamiento.
    /// </summary>
    /// <param name="cliente">Instancia de cliente a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente tal como fue guardado (con ID generado si era nuevo).</returns>
    Task<Cliente> GuardarAsync(Cliente cliente, CancellationToken ct = default);
}
