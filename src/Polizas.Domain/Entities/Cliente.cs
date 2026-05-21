using Polizas.Domain.Exceptions;

namespace Polizas.Domain.Entities;

/// <summary>
/// Representa al tomador o asegurado principal de una póliza de seguro.
/// </summary>
public sealed class Cliente
{
    /// <summary>Identificador único del cliente (generado por la base de datos).</summary>
    public long Id { get; private set; }

    /// <summary>Nombre completo del cliente.</summary>
    public string Nombre { get; private set; } = "";

    /// <summary>Número de documento de identidad del cliente (único en el sistema).</summary>
    public string Documento { get; private set; } = "";

    /// <summary>Dirección de correo electrónico del cliente. Puede ser nula.</summary>
    public string? Email { get; private set; }

    /// <summary>Número de teléfono de contacto del cliente. Puede ser nulo.</summary>
    public string? Telefono { get; private set; }

    /// <summary>Fecha y hora de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Fecha y hora de la última modificación del registro en UTC.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Constructor sin parámetros requerido por EF Core para la materialización de entidades.
    /// No debe utilizarse directamente en el código de la aplicación.
    /// </summary>
    private Cliente() { }

    /// <summary>
    /// Crea y valida un nuevo cliente con los datos proporcionados.
    /// </summary>
    /// <param name="nombre">Nombre completo del cliente. No puede estar vacío.</param>
    /// <param name="documento">Número de documento de identidad. No puede estar vacío.</param>
    /// <param name="email">Correo electrónico opcional del cliente.</param>
    /// <param name="telefono">Teléfono de contacto opcional del cliente.</param>
    /// <returns>Una nueva instancia válida de <see cref="Cliente"/>.</returns>
    /// <exception cref="DomainValidationException">
    /// Se lanza cuando <paramref name="nombre"/> o <paramref name="documento"/> están vacíos o son nulos.
    /// </exception>
    public static Cliente Crear(string nombre, string documento, string? email, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainValidationException("El nombre del cliente es obligatorio.");

        if (string.IsNullOrWhiteSpace(documento))
            throw new DomainValidationException("El documento del cliente es obligatorio.");

        var now = DateTime.UtcNow;

        return new Cliente
        {
            Nombre = nombre.Trim(),
            Documento = documento.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
