namespace Polizas.Application.DTOs;

/// <summary>
/// Representación de una póliza devuelta por la API hacia los consumidores,
/// incluyendo el listado de sus beneficiarios.
/// </summary>
public sealed class PolizaResponse
{
    /// <summary>Identificador único de la póliza.</summary>
    public long Id { get; init; }

    /// <summary>Identificador del cliente titular de la póliza.</summary>
    public long ClienteId { get; init; }

    /// <summary>Valor total de la prima de la póliza.</summary>
    public decimal PrimaTotal { get; init; }

    /// <summary>Fecha de inicio de vigencia de la póliza.</summary>
    public DateOnly FechaEmision { get; init; }

    /// <summary>Fecha de fin de vigencia de la póliza.</summary>
    public DateOnly FechaVencimiento { get; init; }

    /// <summary>
    /// Estado de vigencia de la póliza como cadena de texto
    /// (p. ej., <c>"Activa"</c>, <c>"Vencida"</c>, <c>"Cancelada"</c>).
    /// </summary>
    public string Estado { get; init; } = "";

    /// <summary>Lista de beneficiarios asociados a la póliza.</summary>
    public List<BeneficiarioResponse> Beneficiarios { get; init; } = [];
}
