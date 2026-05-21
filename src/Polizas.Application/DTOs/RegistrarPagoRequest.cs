namespace Polizas.Application.DTOs;

/// <summary>
/// Datos del cuerpo de la solicitud para registrar un pago sobre una póliza.
/// La clave de idempotencia (<c>Idempotency-Key</c>) se recibe como header HTTP,
/// no como parte de este DTO.
/// </summary>
public sealed class RegistrarPagoRequest
{
    /// <summary>Monto del pago a registrar. Debe ser mayor que cero.</summary>
    public decimal Monto { get; init; }
}
