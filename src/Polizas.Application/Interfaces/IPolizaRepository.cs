using Polizas.Application.DTOs;
using Polizas.Domain.Entities;

namespace Polizas.Application.Interfaces;

/// <summary>
/// Puerto de salida (driven port) que define las operaciones de persistencia
/// para el agregado <see cref="Poliza"/>.
/// La implementación concreta vive en la capa de Infrastructure.
/// </summary>
public interface IPolizaRepository
{
    /// <summary>
    /// Obtiene una póliza por su identificador único (sin cargar colecciones relacionadas).
    /// </summary>
    /// <param name="id">Identificador de la póliza a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La póliza encontrada, o <c>null</c> si no existe.</returns>
    Task<Poliza?> ObtenerPorIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Obtiene la póliza incluyendo sus pagos y beneficiarios mediante eager loading.
    /// Usar cuando se necesita calcular estado de cartera o mostrar el detalle completo.
    /// </summary>
    /// <param name="id">Identificador de la póliza a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La póliza con sus relaciones cargadas, o <c>null</c> si no existe.</returns>
    Task<Poliza?> ObtenerPorIdConDetallesAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Persiste una póliza nueva o actualizada en el almacenamiento.
    /// La operación debe guardar en cascada los beneficiarios y la tabla de unión.
    /// </summary>
    /// <param name="poliza">Instancia de póliza a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La póliza tal como fue guardada (con ID generado si era nueva).</returns>
    Task<Poliza> GuardarAsync(Poliza poliza, CancellationToken ct = default);

    /// <summary>
    /// Retorna las pólizas con saldo pendiente cuya fecha de vencimiento superó
    /// los 30 días en la zona horaria <c>America/Bogota</c>.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// Lista de <see cref="CarteraVencidaDto"/> proyectados directamente desde la base de datos.
    /// </returns>
    Task<IReadOnlyList<CarteraVencidaDto>> ObtenerCarteraVencidaAsync(CancellationToken ct = default);
}
