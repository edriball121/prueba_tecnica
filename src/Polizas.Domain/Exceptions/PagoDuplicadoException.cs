namespace Polizas.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando se intenta registrar un pago con una
/// <c>idempotency_key</c> que ya existe en el sistema.
/// </summary>
public sealed class PagoDuplicadoException : DomainException
{
    /// <summary>Clave de idempotencia que originó el conflicto.</summary>
    public string IdempotencyKey { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PagoDuplicadoException"/>
    /// indicando la clave duplicada.
    /// </summary>
    /// <param name="idempotencyKey">Valor de la clave de idempotencia ya registrada.</param>
    public PagoDuplicadoException(string idempotencyKey)
        : base($"Ya existe un pago registrado con la idempotency key '{idempotencyKey}'.")
    {
        IdempotencyKey = idempotencyKey;
    }
}
