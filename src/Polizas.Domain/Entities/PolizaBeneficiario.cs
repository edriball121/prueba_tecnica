namespace Polizas.Domain.Entities;

/// <summary>
/// Entidad de unión (junction entity) que representa la asociación many-to-many
/// entre una <see cref="Poliza"/> y un <see cref="Beneficiario"/>.
/// Almacena además el parentesco del beneficiario con el tomador de la póliza.
/// </summary>
public sealed class PolizaBeneficiario
{
    /// <summary>Identificador de la póliza asociada.</summary>
    public long PolizaId { get; private set; }

    /// <summary>Identificador del beneficiario asociado.</summary>
    public long BeneficiarioId { get; private set; }

    /// <summary>
    /// Parentesco del beneficiario con el tomador de la póliza.
    /// Puede ser nulo si no se especifica.
    /// </summary>
    public string? Parentesco { get; private set; }

    /// <summary>Fecha y hora de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Propiedad de navegación hacia el beneficiario asociado.
    /// Populada por EF Core mediante eager loading.
    /// </summary>
    public Beneficiario Beneficiario { get; private set; } = null!;

    /// <summary>
    /// Constructor sin parámetros requerido por EF Core para la materialización de entidades.
    /// No debe utilizarse directamente en el código de la aplicación.
    /// </summary>
    private PolizaBeneficiario() { }

    /// <summary>
    /// Crea una nueva asociación entre una póliza y un beneficiario.
    /// </summary>
    /// <param name="polizaId">Identificador de la póliza.</param>
    /// <param name="beneficiarioId">Identificador del beneficiario.</param>
    /// <param name="parentesco">Parentesco opcional del beneficiario con el tomador.</param>
    /// <returns>Una nueva instancia de <see cref="PolizaBeneficiario"/>.</returns>
    public static PolizaBeneficiario Crear(long polizaId, long beneficiarioId, string? parentesco)
    {
        return new PolizaBeneficiario
        {
            PolizaId = polizaId,
            BeneficiarioId = beneficiarioId,
            Parentesco = string.IsNullOrWhiteSpace(parentesco) ? null : parentesco.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
