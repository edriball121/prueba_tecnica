namespace Polizas.Application.DTOs;

/// <summary>
/// Datos requeridos para crear una nueva póliza de seguro en el sistema.
/// </summary>
public sealed class CreatePolizaRequest
{
    /// <summary>Identificador del cliente titular de la póliza. El cliente debe existir previamente.</summary>
    public long ClienteId { get; init; }

    /// <summary>Valor total de la prima de la póliza. Debe ser mayor que cero.</summary>
    public decimal PrimaTotal { get; init; }

    /// <summary>Fecha de inicio de vigencia de la póliza.</summary>
    public DateOnly FechaEmision { get; init; }

    /// <summary>
    /// Fecha de fin de vigencia de la póliza.
    /// Debe ser posterior a <see cref="FechaEmision"/>.
    /// </summary>
    public DateOnly FechaVencimiento { get; init; }

    /// <summary>
    /// Lista de beneficiarios a asociar con la póliza.
    /// Los beneficiarios ya existentes (por documento) serán reutilizados.
    /// </summary>
    public List<BeneficiarioRequest> Beneficiarios { get; init; } = [];
}
