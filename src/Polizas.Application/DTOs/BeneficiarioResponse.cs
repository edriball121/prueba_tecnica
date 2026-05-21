namespace Polizas.Application.DTOs;

/// <summary>
/// Representación de un beneficiario dentro de la respuesta de una póliza.
/// </summary>
public sealed class BeneficiarioResponse
{
    /// <summary>Identificador único del beneficiario.</summary>
    public long Id { get; init; }

    /// <summary>Nombre completo del beneficiario.</summary>
    public string Nombre { get; init; } = "";

    /// <summary>Número de documento de identidad del beneficiario.</summary>
    public string Documento { get; init; } = "";

    /// <summary>
    /// Parentesco del beneficiario con el titular de la póliza.
    /// Puede ser nulo si no fue especificado.
    /// </summary>
    public string? Parentesco { get; init; }
}
