namespace Polizas.Domain.Exceptions;

/// <summary>
/// Clase base abstracta para todas las excepciones originadas en la capa de dominio.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DomainException"/> con el mensaje especificado.
    /// </summary>
    /// <param name="message">Descripción del error de dominio.</param>
    protected DomainException(string message) : base(message) { }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DomainException"/> con mensaje e inner exception.
    /// </summary>
    /// <param name="message">Descripción del error de dominio.</param>
    /// <param name="innerException">Excepción que originó este error.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
