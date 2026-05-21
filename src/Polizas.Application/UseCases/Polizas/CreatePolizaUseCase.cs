using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Domain.Exceptions;

namespace Polizas.Application.UseCases.Polizas;

/// <summary>
/// Caso de uso para la creación de una nueva póliza de seguro.
/// Verifica la existencia del cliente, gestiona beneficiarios de forma upsert
/// (reutiliza si ya existen por documento, crea si no) y persiste el agregado completo.
/// </summary>
public sealed class CreatePolizaUseCase
{
    private readonly IPolizaRepository _polizaRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IBeneficiarioRepository _beneficiarioRepo;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CreatePolizaUseCase"/>
    /// con los repositorios requeridos.
    /// </summary>
    /// <param name="polizaRepo">Repositorio de pólizas (puerto de salida).</param>
    /// <param name="clienteRepo">Repositorio de clientes (puerto de salida).</param>
    /// <param name="beneficiarioRepo">Repositorio de beneficiarios (puerto de salida).</param>
    public CreatePolizaUseCase(
        IPolizaRepository polizaRepo,
        IClienteRepository clienteRepo,
        IBeneficiarioRepository beneficiarioRepo)
    {
        _polizaRepo = polizaRepo;
        _clienteRepo = clienteRepo;
        _beneficiarioRepo = beneficiarioRepo;
    }

    /// <summary>
    /// Crea una póliza para un cliente existente junto con sus beneficiarios.
    /// Si un beneficiario ya existe en el sistema (identificado por documento), se reutiliza;
    /// de lo contrario, se crea uno nuevo antes de asociarlo.
    /// </summary>
    /// <param name="request">Datos de la póliza a crear, incluyendo beneficiarios.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Representación de la póliza recién creada con sus beneficiarios.</returns>
    /// <exception cref="ClienteNoEncontradoException">
    /// Se lanza cuando el cliente especificado en <paramref name="request"/> no existe.
    /// </exception>
    public async Task<PolizaResponse> EjecutarAsync(CreatePolizaRequest request, CancellationToken ct)
    {
        // 1. Verificar existencia del cliente
        var cliente = await _clienteRepo.ObtenerPorIdAsync(request.ClienteId, ct);
        if (cliente is null)
            throw new ClienteNoEncontradoException(request.ClienteId);

        // 2. Crear el agregado Poliza (sin ID todavía)
        var poliza = Poliza.Crear(
            request.ClienteId,
            request.PrimaTotal,
            request.FechaEmision,
            request.FechaVencimiento);

        // 3. Guardar la póliza para obtener su ID generado por la BD
        var polizaGuardada = await _polizaRepo.GuardarAsync(poliza, ct);

        // 4. Procesar beneficiarios: upsert por documento y asociar a la póliza
        foreach (var benefReq in request.Beneficiarios)
        {
            var beneficiario = await _beneficiarioRepo.ObtenerPorDocumentoAsync(benefReq.Documento, ct);

            if (beneficiario is null)
            {
                beneficiario = Beneficiario.Crear(benefReq.Nombre, benefReq.Documento, benefReq.Email);
                beneficiario = await _beneficiarioRepo.GuardarAsync(beneficiario, ct);
            }

            var pb = PolizaBeneficiario.Crear(polizaGuardada.Id, beneficiario.Id, benefReq.Parentesco);
            polizaGuardada.AgregarBeneficiario(pb);
        }

        // 5. Guardar nuevamente para persistir las asociaciones de beneficiarios
        var polizaFinal = await _polizaRepo.GuardarAsync(polizaGuardada, ct);

        // 6. Recargar con detalles para mapear la respuesta
        var polizaConDetalles = await _polizaRepo.ObtenerPorIdConDetallesAsync(polizaFinal.Id, ct)
            ?? polizaFinal;

        return MapearResponse(polizaConDetalles);
    }

    /// <summary>
    /// Mapea el agregado <see cref="Poliza"/> al DTO de respuesta <see cref="PolizaResponse"/>.
    /// </summary>
    /// <param name="poliza">Entidad a mapear (debe tener beneficiarios cargados).</param>
    /// <returns>DTO de respuesta populado.</returns>
    private static PolizaResponse MapearResponse(Poliza poliza) => new()
    {
        Id = poliza.Id,
        ClienteId = poliza.ClienteId,
        PrimaTotal = poliza.PrimaTotal,
        FechaEmision = poliza.FechaEmision,
        FechaVencimiento = poliza.FechaVencimiento,
        Estado = poliza.Estado.ToString(),
        Beneficiarios = poliza.Beneficiarios
            .Select(pb => new BeneficiarioResponse
            {
                Id = pb.BeneficiarioId,
                Nombre = pb.Beneficiario?.Nombre ?? "",
                Documento = pb.Beneficiario?.Documento ?? "",
                Parentesco = pb.Parentesco
            })
            .ToList()
    };
}
