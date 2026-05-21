namespace Polizas.Application.DTOs;

/// <summary>
/// Datos requeridos para crear un nuevo cliente en el sistema.
/// </summary>
public sealed class CreateClienteRequest
{
    /// <summary>Nombre completo del cliente. Campo obligatorio.</summary>
    public string Nombre { get; init; } = "";

    /// <summary>Número de documento de identidad del cliente. Campo obligatorio y único.</summary>
    public string Documento { get; init; } = "";

    /// <summary>Dirección de correo electrónico del cliente. Campo opcional.</summary>
    public string? Email { get; init; }

    /// <summary>Número de teléfono de contacto del cliente. Campo opcional.</summary>
    public string? Telefono { get; init; }
}
