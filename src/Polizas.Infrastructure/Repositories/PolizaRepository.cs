using Microsoft.EntityFrameworkCore;
using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;
using Polizas.Domain.Entities;
using Polizas.Infrastructure.Data;

namespace Polizas.Infrastructure.Repositories;

/// <summary>
/// Implementación concreta del puerto de salida <see cref="IPolizaRepository"/>
/// usando EF Core con PostgreSQL a través de <see cref="PolizasDbContext"/>.
/// Implementa el patrón de dos pasadas requerido por <c>CreatePolizaUseCase</c>.
/// </summary>
public sealed class PolizaRepository : IPolizaRepository
{
    private readonly PolizasDbContext _context;

    /// <summary>
    /// Inicializa el repositorio con el contexto de base de datos inyectado.
    /// </summary>
    /// <param name="context">DbContext de la aplicación.</param>
    public PolizaRepository(PolizasDbContext context) => _context = context;

    /// <summary>
    /// Obtiene una póliza por su identificador único sin cargar colecciones relacionadas.
    /// Usar cuando solo se necesitan los datos básicos de la póliza.
    /// </summary>
    /// <param name="id">Identificador de la póliza a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La póliza encontrada, o <c>null</c> si no existe.</returns>
    public async Task<Poliza?> ObtenerPorIdAsync(long id, CancellationToken ct = default)
        => await _context.Polizas.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>
    /// Obtiene una póliza incluyendo sus pagos y beneficiarios mediante eager loading.
    /// Usar cuando se necesita calcular el estado de cartera o mostrar el detalle completo.
    /// </summary>
    /// <param name="id">Identificador de la póliza a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La póliza con sus relaciones cargadas, o <c>null</c> si no existe.</returns>
    public async Task<Poliza?> ObtenerPorIdConDetallesAsync(long id, CancellationToken ct = default)
        => await _context.Polizas
            .Include(p => p.Pagos)
            .Include(p => p.Beneficiarios)
                .ThenInclude(pb => pb.Beneficiario)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>
    /// Persiste una póliza nueva o actualizada en la base de datos.
    /// Implementa el patrón de dos pasadas requerido por <c>CreatePolizaUseCase</c>:
    /// <list type="bullet">
    ///   <item>Primera pasada (Id == 0): agrega la póliza y obtiene el ID generado.</item>
    ///   <item>Segunda pasada (Id > 0): detecta los <see cref="PolizaBeneficiario"/> en estado
    ///   Detached y los agrega explícitamente antes de guardar.</item>
    /// </list>
    /// </summary>
    /// <param name="poliza">Instancia de póliza a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La póliza guardada con el ID asignado por la base de datos.</returns>
    public async Task<Poliza> GuardarAsync(Poliza poliza, CancellationToken ct = default)
    {
        if (poliza.Id == 0)
        {
            // Primera pasada: póliza nueva, sin beneficiarios aún
            _context.Add(poliza);
        }
        else
        {
            // Segunda pasada: la póliza ya está tracked; agregar los PolizaBeneficiarios
            // que vengan en estado Detached (recién creados por el use case)
            foreach (var pb in poliza.Beneficiarios)
            {
                if (_context.Entry(pb).State == EntityState.Detached)
                    _context.Add(pb);
            }
        }

        await _context.SaveChangesAsync(ct);
        return poliza;
    }

    /// <summary>
    /// Retorna las pólizas con saldo pendiente cuya fecha de vencimiento superó
    /// los 30 días en la zona horaria <c>America/Bogota</c>.
    /// </summary>
    /// <remarks>
    /// El cálculo se realiza en dos pasos:
    /// <list type="number">
    ///   <item>Consulta en BD: pólizas con <c>FechaVencimiento</c> anterior al corte de 30 días,
    ///   incluyendo sus pagos.</item>
    ///   <item>Filtro en memoria: se descartan pólizas cuyo saldo pendiente sea cero o negativo.</item>
    /// </list>
    /// </remarks>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de <see cref="CarteraVencidaDto"/> proyectados desde la base de datos.</returns>
    public async Task<IReadOnlyList<CarteraVencidaDto>> ObtenerCarteraVencidaAsync(CancellationToken ct = default)
    {
        var fechaHoyBogota = ObtenerFechaHoyBogota();
        var cutoffDate = fechaHoyBogota.AddDays(-30);

        // Consulta a BD: pólizas cuya fecha de vencimiento superó el corte de 30 días
        var polizasVencidas = await _context.Polizas
            .Include(p => p.Pagos)
            .Where(p => p.FechaVencimiento < cutoffDate)
            .AsNoTracking()
            .ToListAsync(ct);

        // Filtro en memoria: solo las que tengan saldo pendiente
        var polizasConDeuda = polizasVencidas
            .Where(p => p.CalcularSaldoPendiente() > 0)
            .ToList();

        if (polizasConDeuda.Count == 0)
            return [];

        // Obtener los clientes de las pólizas en cartera vencida
        var clienteIds = polizasConDeuda.Select(p => p.ClienteId).Distinct().ToList();
        var clientes = await _context.Clientes
            .Where(c => clienteIds.Contains(c.Id))
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, ct);

        // Mapear a CarteraVencidaDto
        var resultado = polizasConDeuda
            .Select(p =>
            {
                var totalPagado = p.CalcularTotalPagado();
                var saldoPendiente = p.PrimaTotal - totalPagado;
                var diasMora = (fechaHoyBogota.ToDateTime(TimeOnly.MinValue) - p.FechaVencimiento.ToDateTime(TimeOnly.MinValue)).Days;

                clientes.TryGetValue(p.ClienteId, out var cliente);

                return new CarteraVencidaDto
                {
                    PolizaId = p.Id,
                    ClienteId = p.ClienteId,
                    ClienteNombre = cliente?.Nombre ?? "",
                    ClienteDocumento = cliente?.Documento ?? "",
                    PrimaTotal = p.PrimaTotal,
                    TotalPagado = totalPagado,
                    SaldoPendiente = saldoPendiente,
                    DiasMora = diasMora
                };
            })
            .ToList();

        return resultado.AsReadOnly();
    }

    /// <summary>
    /// Obtiene la fecha actual en la zona horaria de Bogotá (America/Bogota).
    /// En Windows usa el identificador IANA; si no está disponible, usa el identificador
    /// de Windows <c>SA Pacific Standard Time</c> como fallback.
    /// </summary>
    /// <returns>La fecha actual como <see cref="DateOnly"/> en zona horaria de Bogotá.</returns>
    private static DateOnly ObtenerFechaHoyBogota()
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
    }
}
