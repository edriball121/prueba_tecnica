namespace Polizas.Application.DTOs;

/// <summary>
/// Respuesta detallada sobre el estado de cartera de una póliza,
/// incluyendo información de pagos y mora calculada en zona horaria de Bogotá.
/// </summary>
public sealed class PolizaEstadoResponse
{
    /// <summary>Identificador único de la póliza consultada.</summary>
    public long PolizaId { get; init; }

    /// <summary>
    /// Estado de cartera de la póliza como cadena normalizada:
    /// <c>"al_dia"</c> o <c>"en_mora"</c>.
    /// </summary>
    public string EstadoCartera { get; init; } = "";

    /// <summary>Valor total de la prima de la póliza.</summary>
    public decimal PrimaTotal { get; init; }

    /// <summary>Suma de todos los pagos registrados sobre la póliza.</summary>
    public decimal TotalPagado { get; init; }

    /// <summary>Diferencia entre la prima total y el total pagado.</summary>
    public decimal SaldoPendiente { get; init; }

    /// <summary>
    /// Fecha y hora del último pago registrado en UTC.
    /// Es <c>null</c> si no se han realizado pagos.
    /// </summary>
    public DateTime? FechaUltimoPago { get; init; }

    /// <summary>
    /// Número de días transcurridos desde la fecha de vencimiento.
    /// Es <c>0</c> cuando el estado es <c>"al_dia"</c>.
    /// </summary>
    public int DiasMora { get; init; }
}
