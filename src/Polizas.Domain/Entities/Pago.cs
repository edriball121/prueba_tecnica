using Polizas.Domain.Exceptions;

namespace Polizas.Domain.Entities;

/// <summary>
/// Representa un pago realizado sobre una póliza de seguro.
/// Cada pago es idempotente mediante la propiedad <see cref="IdempotencyKey"/>.
/// </summary>
public sealed class Pago
{
    /// <summary>Identificador único del pago (generado por la base de datos).</summary>
    public long Id { get; private set; }

    /// <summary>Identificador de la póliza a la que pertenece este pago.</summary>
    public long PolizaId { get; private set; }

    /// <summary>Monto del pago. Siempre mayor que cero.</summary>
    public decimal Monto { get; private set; }

    /// <summary>Fecha y hora en que se realizó el pago, almacenada en UTC.</summary>
    public DateTime FechaPago { get; private set; }

    /// <summary>
    /// Clave de idempotencia provista por el cliente (vía header HTTP <c>Idempotency-Key</c>).
    /// Garantiza que el mismo pago no se registre más de una vez.
    /// </summary>
    public string IdempotencyKey { get; private set; } = "";

    /// <summary>Fecha y hora de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Constructor sin parámetros requerido por EF Core para la materialización de entidades.
    /// No debe utilizarse directamente en el código de la aplicación.
    /// </summary>
    private Pago() { }

    /// <summary>
    /// Crea y valida un nuevo pago para la póliza indicada.
    /// </summary>
    /// <param name="polizaId">Identificador de la póliza sobre la que se aplica el pago.</param>
    /// <param name="monto">Monto del pago. Debe ser mayor que cero.</param>
    /// <param name="idempotencyKey">Clave de idempotencia única para este pago. No puede estar vacía.</param>
    /// <returns>Una nueva instancia válida de <see cref="Pago"/>.</returns>
    /// <exception cref="DomainValidationException">
    /// Se lanza cuando <paramref name="monto"/> es menor o igual a cero,
    /// o cuando <paramref name="idempotencyKey"/> está vacía o es nula.
    /// </exception>
    public static Pago Crear(long polizaId, decimal monto, string idempotencyKey)
    {
        if (monto <= 0)
            throw new DomainValidationException("El monto del pago debe ser mayor que cero.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainValidationException("La idempotency key del pago es obligatoria.");

        var now = DateTime.UtcNow;

        return new Pago
        {
            PolizaId = polizaId,
            Monto = monto,
            IdempotencyKey = idempotencyKey.Trim(),
            FechaPago = now,
            CreatedAt = now
        };
    }
}
