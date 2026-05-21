using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Domain.Exceptions;

namespace Polizas.Application.UseCases.Pagos;

/// <summary>
/// Caso de uso para registrar un pago sobre una póliza de forma idempotente.
/// Utiliza el header <c>Idempotency-Key</c> para evitar pagos duplicados.
/// </summary>
public sealed class RegistrarPagoUseCase
{
    private readonly IPolizaRepository _polizaRepo;
    private readonly IPagoRepository _pagoRepo;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RegistrarPagoUseCase"/>
    /// con los repositorios requeridos.
    /// </summary>
    /// <param name="polizaRepo">Repositorio de pólizas (puerto de salida).</param>
    /// <param name="pagoRepo">Repositorio de pagos (puerto de salida).</param>
    public RegistrarPagoUseCase(IPolizaRepository polizaRepo, IPagoRepository pagoRepo)
    {
        _polizaRepo = polizaRepo;
        _pagoRepo = pagoRepo;
    }

    /// <summary>
    /// Registra un pago sobre la póliza indicada y retorna el estado actualizado de la misma.
    /// </summary>
    /// <param name="polizaId">Identificador de la póliza sobre la que se aplica el pago.</param>
    /// <param name="request">Datos del pago (monto).</param>
    /// <param name="idempotencyKey">
    /// Clave de idempotencia provista por el cliente vía header HTTP <c>Idempotency-Key</c>.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Estado actualizado de la póliza tras registrar el pago.</returns>
    /// <exception cref="PagoDuplicadoException">
    /// Se lanza cuando ya existe un pago con la misma <paramref name="idempotencyKey"/>.
    /// </exception>
    /// <exception cref="PolizaNoEncontradaException">
    /// Se lanza cuando la póliza con <paramref name="polizaId"/> no existe.
    /// </exception>
    public async Task<PolizaEstadoResponse> EjecutarAsync(
        long polizaId,
        RegistrarPagoRequest request,
        string idempotencyKey,
        CancellationToken ct)
    {
        // 1. Verificar idempotencia antes de cualquier otra operación
        var duplicado = await _pagoRepo.ExisteIdempotencyKeyAsync(idempotencyKey, ct);
        if (duplicado)
            throw new PagoDuplicadoException(idempotencyKey);

        // 2. Verificar existencia de la póliza
        var poliza = await _polizaRepo.ObtenerPorIdAsync(polizaId, ct);
        if (poliza is null)
            throw new PolizaNoEncontradaException(polizaId);

        // 3. Crear y persistir el pago
        var pago = Pago.Crear(polizaId, request.Monto, idempotencyKey);
        await _pagoRepo.GuardarAsync(pago, ct);

        // 4. Recargar la póliza con todos sus pagos para calcular el estado actualizado
        var polizaConDetalles = await _polizaRepo.ObtenerPorIdConDetallesAsync(polizaId, ct)
            ?? poliza;

        return MapearEstadoResponse(polizaConDetalles);
    }

    /// <summary>
    /// Mapea el agregado <see cref="Poliza"/> al DTO <see cref="PolizaEstadoResponse"/>.
    /// El estado de cartera se calcula sin zona horaria específica en este método;
    /// el estado de mora exacto debe consultarse a través de <c>GetPolizaEstadoUseCase</c>.
    /// En este método se retorna el estado basado en los datos de la póliza sin ajuste de TZ.
    /// </summary>
    /// <param name="poliza">Póliza con pagos cargados.</param>
    /// <returns>DTO de estado de la póliza.</returns>
    private static PolizaEstadoResponse MapearEstadoResponse(Poliza poliza)
    {
        var totalPagado = poliza.CalcularTotalPagado();
        var saldoPendiente = poliza.CalcularSaldoPendiente();
        var fechaUltimoPago = poliza.Pagos.Count > 0
            ? poliza.Pagos.Max(p => p.FechaPago)
            : (DateTime?)null;

        // Calcular estado usando fecha local del sistema (UTC como referencia neutral)
        // El caso de uso especializado GetPolizaEstadoUseCase aplica la TZ de Bogotá.
        var fechaHoyUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var estadoCartera = poliza.ObtenerEstadoCartera(fechaHoyUtc);

        var diasMora = 0;
        if (estadoCartera == Domain.Enums.EstadoCartera.EnMora)
        {
            diasMora = fechaHoyUtc.DayNumber - poliza.FechaVencimiento.DayNumber;
            if (diasMora < 0) diasMora = 0;
        }

        return new PolizaEstadoResponse
        {
            PolizaId = poliza.Id,
            EstadoCartera = estadoCartera == Domain.Enums.EstadoCartera.AlDia ? "al_dia" : "en_mora",
            PrimaTotal = poliza.PrimaTotal,
            TotalPagado = totalPagado,
            SaldoPendiente = saldoPendiente,
            FechaUltimoPago = fechaUltimoPago,
            DiasMora = diasMora
        };
    }
}
