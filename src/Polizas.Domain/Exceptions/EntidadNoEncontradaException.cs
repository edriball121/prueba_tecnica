namespace Polizas.Domain.Exceptions;

/// <summary>
/// Excepción base para los casos en que una entidad solicitada no existe en el sistema.
/// </summary>
public abstract class EntidadNoEncontradaException : DomainException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EntidadNoEncontradaException"/>
    /// con el mensaje especificado.
    /// </summary>
    /// <param name="message">Descripción de qué entidad no fue encontrada.</param>
    protected EntidadNoEncontradaException(string message) : base(message) { }
}
