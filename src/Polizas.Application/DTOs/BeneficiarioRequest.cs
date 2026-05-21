namespace Polizas.Application.DTOs;

/// <summary>
/// Datos de un beneficiario incluido en la solicitud de creación de una póliza.
/// Si el beneficiario ya existe en el sistema (por documento), se reutiliza;
/// de lo contrario, se crea uno nuevo.
/// </summary>
public sealed class BeneficiarioRequest
{
    /// <summary>Nombre completo del beneficiario. Campo obligatorio.</summary>
    public string Nombre { get; init; } = "";

    /// <summary>Número de documento de identidad del beneficiario. Campo obligatorio.</summary>
    public string Documento { get; init; } = "";

    /// <summary>Dirección de correo electrónico del beneficiario. Campo opcional.</summary>
    public string? Email { get; init; }

    /// <summary>
    /// Parentesco del beneficiario con el titular de la póliza (p. ej., "Cónyuge", "Hijo").
    /// Campo opcional.
    /// </summary>
    public string? Parentesco { get; init; }
}
