namespace Polizas.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando no se encuentra una póliza con el identificador especificado.
/// </summary>
public sealed class PolizaNoEncontradaException : EntidadNoEncontradaException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PolizaNoEncontradaException"/>
    /// para una póliza buscada por ID.
    /// </summary>
    /// <param name="id">Identificador de la póliza que no fue encontrada.</param>
    public PolizaNoEncontradaException(long id)
        : base($"No se encontró una póliza con el ID '{id}'.") { }
}
