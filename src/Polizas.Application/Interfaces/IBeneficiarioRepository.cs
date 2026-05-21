using Polizas.Domain.Entities;

namespace Polizas.Application.Interfaces;

/// <summary>
/// Puerto de salida (driven port) que define las operaciones de persistencia
/// para la entidad <see cref="Beneficiario"/>.
/// La implementación concreta vive en la capa de Infrastructure.
/// </summary>
public interface IBeneficiarioRepository
{
    /// <summary>
    /// Obtiene un beneficiario por su número de documento de identidad.
    /// </summary>
    /// <param name="documento">Documento de identidad a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El beneficiario encontrado, o <c>null</c> si no existe.</returns>
    Task<Beneficiario?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct = default);

    /// <summary>
    /// Persiste un beneficiario nuevo o actualizado en el almacenamiento.
    /// </summary>
    /// <param name="beneficiario">Instancia de beneficiario a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El beneficiario tal como fue guardado (con ID generado si era nuevo).</returns>
    Task<Beneficiario> GuardarAsync(Beneficiario beneficiario, CancellationToken ct = default);
}
