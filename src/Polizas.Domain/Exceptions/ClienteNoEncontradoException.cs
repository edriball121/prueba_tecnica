namespace Polizas.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando no se encuentra un cliente con el identificador especificado.
/// </summary>
public sealed class ClienteNoEncontradoException : EntidadNoEncontradaException
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ClienteNoEncontradoException"/>
    /// para un cliente buscado por ID.
    /// </summary>
    /// <param name="id">Identificador del cliente que no fue encontrado.</param>
    public ClienteNoEncontradoException(long id)
        : base($"No se encontró un cliente con el ID '{id}'.") { }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ClienteNoEncontradoException"/>
    /// para un cliente buscado por documento.
    /// </summary>
    /// <param name="documento">Documento de identidad del cliente que no fue encontrado.</param>
    public ClienteNoEncontradoException(string documento)
        : base($"No se encontró un cliente con el documento '{documento}'.") { }
}
