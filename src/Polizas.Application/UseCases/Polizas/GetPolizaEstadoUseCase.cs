using Microsoft.Extensions.Options;
using Polizas.Application.DTOs;
using Polizas.Application.Options;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Domain.Enums;
using Polizas.Domain.Exceptions;

namespace Polizas.Application.UseCases.Polizas;

/// <summary>
/// Caso de uso para consultar el estado de cartera de una póliza.
/// Aplica la zona horaria <c>America/Bogota</c> (configurable vía <see cref="TimezoneOptions"/>)
/// para determinar si la póliza está al día o en mora.
/// </summary>
public sealed class GetPolizaEstadoUseCase
{
    private readonly IPolizaRepository _polizaRepo;
    private readonly IOptions<TimezoneOptions> _tzOptions;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GetPolizaEstadoUseCase"/>
    /// con el repositorio y las opciones de zona horaria.
    /// </summary>
    /// <param name="polizaRepo">Repositorio de pólizas (puerto de salida).</param>
    /// <param name="tzOptions">Opciones de zona horaria enlazadas desde la configuración.</param>
    public GetPolizaEstadoUseCase(IPolizaRepository polizaRepo, IOptions<TimezoneOptions> tzOptions)
    {
        _polizaRepo = polizaRepo;
        _tzOptions = tzOptions;
    }

    /// <summary>
    /// Calcula el estado de cartera de la póliza indicada usando la zona horaria de Bogotá.
    /// </summary>
    /// <param name="polizaId">Identificador de la póliza a consultar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Estado de cartera detallado de la póliza.</returns>
    /// <exception cref="PolizaNoEncontradaException">
    /// Se lanza cuando la póliza con <paramref name="polizaId"/> no existe.
    /// </exception>
    public async Task<PolizaEstadoResponse> EjecutarAsync(long polizaId, CancellationToken ct)
    {
        // 1. Cargar la póliza con sus pagos y beneficiarios
        var poliza = await _polizaRepo.ObtenerPorIdConDetallesAsync(polizaId, ct);
        if (poliza is null)
            throw new PolizaNoEncontradaException(polizaId);

        // 2. Obtener la fecha actual en la zona horaria de Bogotá
        var fechaHoyBogota = ObtenerFechaHoyBogota();

        // 3. Calcular el estado de cartera usando la lógica de dominio
        var estadoCartera = poliza.ObtenerEstadoCartera(fechaHoyBogota);

        // 4. Calcular días de mora si aplica
        var diasMora = 0;
        if (estadoCartera == EstadoCartera.EnMora)
        {
            diasMora = fechaHoyBogota.DayNumber - poliza.FechaVencimiento.DayNumber;
            if (diasMora < 0) diasMora = 0;
        }

        var totalPagado = poliza.CalcularTotalPagado();
        var saldoPendiente = poliza.CalcularSaldoPendiente();
        var fechaUltimoPago = poliza.Pagos.Count > 0
            ? poliza.Pagos.Max(p => p.FechaPago)
            : (DateTime?)null;

        return new PolizaEstadoResponse
        {
            PolizaId = poliza.Id,
            EstadoCartera = estadoCartera == EstadoCartera.AlDia ? "al_dia" : "en_mora",
            PrimaTotal = poliza.PrimaTotal,
            TotalPagado = totalPagado,
            SaldoPendiente = saldoPendiente,
            FechaUltimoPago = fechaUltimoPago,
            DiasMora = diasMora
        };
    }

    /// <summary>
    /// Obtiene la fecha actual en la zona horaria configurada (America/Bogota).
    /// Soporta tanto el identificador IANA (<c>"America/Bogota"</c>) usado en Linux/macOS
    /// como el identificador Windows (<c>"SA Pacific Standard Time"</c>).
    /// </summary>
    /// <returns>Fecha de hoy en la zona horaria de Bogotá.</returns>
    private DateOnly ObtenerFechaHoyBogota()
    {
        var timeZoneId = _tzOptions.Value.TimeZoneId;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback: intentar el identificador alternativo
            var fallback = timeZoneId == "America/Bogota"
                ? "SA Pacific Standard Time"
                : "America/Bogota";

            tz = TimeZoneInfo.FindSystemTimeZoneById(fallback);
        }

        var ahoraBogota = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(ahoraBogota);
    }
}
