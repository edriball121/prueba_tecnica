namespace Polizas.Application.DTOs;

/// <summary>
/// Proyección plana de una póliza en situación de cartera vencida
/// (saldo pendiente y más de 30 días desde el vencimiento, evaluado en zona horaria de Bogotá).
/// </summary>
public sealed class CarteraVencidaDto
{
    /// <summary>Identificador único de la póliza.</summary>
    public long PolizaId { get; init; }

    /// <summary>Identificador del cliente titular de la póliza.</summary>
    public long ClienteId { get; init; }

    /// <summary>Nombre completo del cliente titular.</summary>
    public string ClienteNombre { get; init; } = "";

    /// <summary>Número de documento de identidad del cliente titular.</summary>
    public string ClienteDocumento { get; init; } = "";

    /// <summary>Valor total de la prima de la póliza.</summary>
    public decimal PrimaTotal { get; init; }

    /// <summary>Suma de todos los pagos registrados sobre la póliza.</summary>
    public decimal TotalPagado { get; init; }

    /// <summary>Diferencia entre la prima total y el total pagado.</summary>
    public decimal SaldoPendiente { get; init; }

    /// <summary>
    /// Número de días transcurridos desde la fecha de vencimiento de la póliza,
    /// calculado en zona horaria <c>America/Bogota</c>.
    /// </summary>
    public int DiasMora { get; init; }
}
