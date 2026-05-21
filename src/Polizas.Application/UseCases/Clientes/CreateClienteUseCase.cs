using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Domain.Exceptions;

namespace Polizas.Application.UseCases.Clientes;

/// <summary>
/// Caso de uso para la creación de un nuevo cliente en el sistema.
/// Garantiza que el documento de identidad sea único antes de persistir.
/// </summary>
public sealed class CreateClienteUseCase
{
    private readonly IClienteRepository _repo;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CreateClienteUseCase"/>
    /// con el repositorio de clientes inyectado.
    /// </summary>
    /// <param name="repo">Repositorio de clientes (puerto de salida).</param>
    public CreateClienteUseCase(IClienteRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Crea un cliente nuevo a partir de los datos del request.
    /// </summary>
    /// <param name="request">Datos del cliente a crear.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Representación del cliente recién creado.</returns>
    /// <exception cref="DomainValidationException">
    /// Se lanza cuando ya existe un cliente con el mismo número de documento.
    /// </exception>
    public async Task<ClienteResponse> EjecutarAsync(CreateClienteRequest request, CancellationToken ct)
    {
        var existente = await _repo.ObtenerPorDocumentoAsync(request.Documento, ct);
        if (existente is not null)
            throw new DomainValidationException(
                $"Ya existe un cliente con el documento '{request.Documento}'.");

        var cliente = Cliente.Crear(request.Nombre, request.Documento, request.Email, request.Telefono);
        var guardado = await _repo.GuardarAsync(cliente, ct);

        return MapearResponse(guardado);
    }

    /// <summary>
    /// Mapea una entidad <see cref="Cliente"/> al DTO de respuesta <see cref="ClienteResponse"/>.
    /// </summary>
    /// <param name="cliente">Entidad a mapear.</param>
    /// <returns>DTO de respuesta populado.</returns>
    private static ClienteResponse MapearResponse(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        Documento = cliente.Documento,
        Email = cliente.Email,
        Telefono = cliente.Telefono,
        CreatedAt = cliente.CreatedAt
    };
}
