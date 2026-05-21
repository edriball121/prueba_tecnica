using Polizas.Domain.Entities;

namespace Polizas.Application.Interfaces;

/// <summary>
/// Puerto de salida (driven port) que define las operaciones de persistencia
/// para la entidad <see cref="Pago"/>.
/// La implementación concreta vive en la capa de Infrastructure.
/// </summary>
public interface IPagoRepository
{
    /// <summary>
    /// Verifica si ya existe un pago registrado con la clave de idempotencia especificada.
    /// </summary>
    /// <param name="idempotencyKey">Clave de idempotencia a verificar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// <c>true</c> si ya existe un pago con esa clave; <c>false</c> en caso contrario.
    /// </returns>
    Task<bool> ExisteIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);

    /// <summary>
    /// Persiste un pago nuevo en el almacenamiento.
    /// </summary>
    /// <param name="pago">Instancia de pago a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El pago tal como fue guardado (con ID generado).</returns>
    Task<Pago> GuardarAsync(Pago pago, CancellationToken ct = default);
}
