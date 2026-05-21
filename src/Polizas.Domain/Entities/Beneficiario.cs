using Polizas.Domain.Exceptions;

namespace Polizas.Domain.Entities;

/// <summary>
/// Representa a una persona designada como beneficiaria de una o más pólizas.
/// Un beneficiario se identifica de forma única por su documento y puede estar
/// asociado a múltiples pólizas.
/// </summary>
public sealed class Beneficiario
{
    /// <summary>Identificador único del beneficiario (generado por la base de datos).</summary>
    public long Id { get; private set; }

    /// <summary>Nombre completo del beneficiario.</summary>
    public string Nombre { get; private set; } = "";

    /// <summary>Número de documento de identidad del beneficiario (único en el sistema).</summary>
    public string Documento { get; private set; } = "";

    /// <summary>Dirección de correo electrónico del beneficiario. Puede ser nula.</summary>
    public string? Email { get; private set; }

    /// <summary>Fecha y hora de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Constructor sin parámetros requerido por EF Core para la materialización de entidades.
    /// No debe utilizarse directamente en el código de la aplicación.
    /// </summary>
    private Beneficiario() { }

    /// <summary>
    /// Crea y valida un nuevo beneficiario con los datos proporcionados.
    /// </summary>
    /// <param name="nombre">Nombre completo del beneficiario. No puede estar vacío.</param>
    /// <param name="documento">Número de documento de identidad. No puede estar vacío.</param>
    /// <param name="email">Correo electrónico opcional del beneficiario.</param>
    /// <returns>Una nueva instancia válida de <see cref="Beneficiario"/>.</returns>
    /// <exception cref="DomainValidationException">
    /// Se lanza cuando <paramref name="nombre"/> o <paramref name="documento"/> están vacíos o son nulos.
    /// </exception>
    public static Beneficiario Crear(string nombre, string documento, string? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainValidationException("El nombre del beneficiario es obligatorio.");

        if (string.IsNullOrWhiteSpace(documento))
            throw new DomainValidationException("El documento del beneficiario es obligatorio.");

        return new Beneficiario
        {
            Nombre = nombre.Trim(),
            Documento = documento.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
