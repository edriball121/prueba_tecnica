namespace Polizas.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando una regla de validación de dominio es violada.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DomainValidationException"/>
    /// con el mensaje de validación especificado.
    /// </summary>
    /// <param name="message">Descripción de la regla de dominio que fue violada.</param>
    public DomainValidationException(string message) : base(message) { }
}
