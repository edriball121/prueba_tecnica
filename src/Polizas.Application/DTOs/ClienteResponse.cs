namespace Polizas.Application.DTOs;

/// <summary>
/// Representación de un cliente devuelta por la API hacia los consumidores.
/// </summary>
public sealed class ClienteResponse
{
    /// <summary>Identificador único del cliente.</summary>
    public long Id { get; init; }

    /// <summary>Nombre completo del cliente.</summary>
    public string Nombre { get; init; } = "";

    /// <summary>Número de documento de identidad del cliente.</summary>
    public string Documento { get; init; } = "";

    /// <summary>Dirección de correo electrónico del cliente. Puede ser nula.</summary>
    public string? Email { get; init; }

    /// <summary>Número de teléfono de contacto del cliente. Puede ser nulo.</summary>
    public string? Telefono { get; init; }

    /// <summary>Fecha y hora de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; init; }
}
