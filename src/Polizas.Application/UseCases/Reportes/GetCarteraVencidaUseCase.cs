using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;

namespace Polizas.Application.UseCases.Reportes;

/// <summary>
/// Caso de uso para obtener el reporte de cartera vencida.
/// Delega en el repositorio la consulta de pólizas con saldo pendiente
/// y más de 30 días de mora, evaluado en zona horaria <c>America/Bogota</c>.
/// </summary>
public sealed class GetCarteraVencidaUseCase
{
    private readonly IPolizaRepository _polizaRepo;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GetCarteraVencidaUseCase"/>
    /// con el repositorio de pólizas.
    /// </summary>
    /// <param name="polizaRepo">Repositorio de pólizas (puerto de salida).</param>
    public GetCarteraVencidaUseCase(IPolizaRepository polizaRepo)
    {
        _polizaRepo = polizaRepo;
    }

    /// <summary>
    /// Retorna todas las pólizas con saldo pendiente cuya fecha de vencimiento
    /// superó los 30 días, calculado en zona horaria <c>America/Bogota</c>.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// Lista de <see cref="CarteraVencidaDto"/> con la información de cada póliza vencida.
    /// La lista puede estar vacía si no hay pólizas en esa condición.
    /// </returns>
    public async Task<IReadOnlyList<CarteraVencidaDto>> EjecutarAsync(CancellationToken ct)
    {
        return await _polizaRepo.ObtenerCarteraVencidaAsync(ct);
    }
}
