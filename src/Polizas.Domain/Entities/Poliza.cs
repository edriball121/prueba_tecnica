using Polizas.Domain.Enums;
using Polizas.Domain.Exceptions;

namespace Polizas.Domain.Entities;

/// <summary>
/// Agregado raíz que representa una póliza de seguro.
/// Encapsula las reglas de negocio relacionadas con el estado de cartera,
/// el cálculo de pagos y la gestión de beneficiarios.
/// </summary>
public sealed class Poliza
{
    /// <summary>Identificador único de la póliza (generado por la base de datos).</summary>
    public long Id { get; private set; }

    /// <summary>Identificador del cliente titular de esta póliza.</summary>
    public long ClienteId { get; private set; }

    /// <summary>Valor total de la prima que debe pagarse por la póliza.</summary>
    public decimal PrimaTotal { get; private set; }

    /// <summary>Fecha de inicio de vigencia de la póliza.</summary>
    public DateOnly FechaEmision { get; private set; }

    /// <summary>Fecha de fin de vigencia de la póliza.</summary>
    public DateOnly FechaVencimiento { get; private set; }

    /// <summary>Estado de vigencia actual de la póliza.</summary>
    public EstadoPoliza Estado { get; private set; }

    /// <summary>Fecha y hora de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Fecha y hora de la última modificación del registro en UTC.</summary>
    public DateTime UpdatedAt { get; private set; }

    private readonly List<PolizaBeneficiario> _beneficiarios = [];

    /// <summary>
    /// Colección de beneficiarios asociados a esta póliza.
    /// Solo lectura desde el exterior del agregado.
    /// </summary>
    public IReadOnlyCollection<PolizaBeneficiario> Beneficiarios => _beneficiarios.AsReadOnly();

    private readonly List<Pago> _pagos = [];

    /// <summary>
    /// Colección de pagos realizados sobre esta póliza.
    /// Solo lectura desde el exterior del agregado.
    /// </summary>
    public IReadOnlyCollection<Pago> Pagos => _pagos.AsReadOnly();

    /// <summary>
    /// Constructor sin parámetros requerido por EF Core para la materialización de entidades.
    /// No debe utilizarse directamente en el código de la aplicación.
    /// </summary>
    private Poliza() { }

    /// <summary>
    /// Crea y valida una nueva póliza con los datos proporcionados.
    /// El estado inicial es siempre <see cref="EstadoPoliza.Activa"/>.
    /// </summary>
    /// <param name="clienteId">Identificador del cliente titular.</param>
    /// <param name="primaTotal">Valor total de la prima. Debe ser mayor que cero.</param>
    /// <param name="fechaEmision">Fecha de inicio de vigencia.</param>
    /// <param name="fechaVencimiento">Fecha de fin de vigencia. Debe ser posterior a <paramref name="fechaEmision"/>.</param>
    /// <returns>Una nueva instancia válida de <see cref="Poliza"/>.</returns>
    /// <exception cref="DomainValidationException">
    /// Se lanza cuando <paramref name="primaTotal"/> es menor o igual a cero,
    /// o cuando <paramref name="fechaVencimiento"/> no es posterior a <paramref name="fechaEmision"/>.
    /// </exception>
    public static Poliza Crear(long clienteId, decimal primaTotal, DateOnly fechaEmision, DateOnly fechaVencimiento)
    {
        if (primaTotal <= 0)
            throw new DomainValidationException("La prima total de la póliza debe ser mayor que cero.");

        if (fechaVencimiento <= fechaEmision)
            throw new DomainValidationException(
                "La fecha de vencimiento debe ser posterior a la fecha de emisión.");

        var now = DateTime.UtcNow;

        return new Poliza
        {
            ClienteId = clienteId,
            PrimaTotal = primaTotal,
            FechaEmision = fechaEmision,
            FechaVencimiento = fechaVencimiento,
            Estado = EstadoPoliza.Activa,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Calcula el total de dinero pagado sumando todos los pagos registrados en la póliza.
    /// </summary>
    /// <returns>Suma de los montos de todos los <see cref="Pago"/> asociados.</returns>
    public decimal CalcularTotalPagado() => Pagos.Sum(p => p.Monto);

    /// <summary>
    /// Calcula el saldo pendiente de la póliza (prima total menos total pagado).
    /// Un valor positivo indica deuda; cero o negativo indica que está saldada.
    /// </summary>
    /// <returns>Diferencia entre <see cref="PrimaTotal"/> y el total pagado.</returns>
    public decimal CalcularSaldoPendiente() => PrimaTotal - CalcularTotalPagado();

    /// <summary>
    /// Determina si la póliza está al día según la zona horaria de Bogotá.
    /// Al día si: ya pagó todo ó la fecha de vencimiento no ha pasado.
    /// En mora si: <c>fechaVencimiento &lt; fechaHoyBogota</c> AND <c>totalPagado &lt; primaTotal</c>.
    /// </summary>
    /// <param name="fechaHoyBogota">
    /// Fecha actual en la zona horaria <c>America/Bogota</c>, obtenida por el llamador
    /// para desacoplar el cálculo de la consulta al sistema operativo.
    /// </param>
    /// <returns>
    /// <see cref="EstadoCartera.EnMora"/> cuando la fecha de vencimiento ya pasó y queda saldo pendiente;
    /// <see cref="EstadoCartera.AlDia"/> en cualquier otro caso.
    /// </returns>
    public EstadoCartera ObtenerEstadoCartera(DateOnly fechaHoyBogota)
    {
        var totalPagado = CalcularTotalPagado();
        var vencida = FechaVencimiento < fechaHoyBogota;
        var conDeuda = totalPagado < PrimaTotal;

        return vencida && conDeuda
            ? EstadoCartera.EnMora
            : EstadoCartera.AlDia;
    }

    /// <summary>
    /// Agrega un <see cref="PolizaBeneficiario"/> a la póliza.
    /// Utilizado por el factory/use case durante la creación de la póliza.
    /// </summary>
    /// <param name="pb">Asociación póliza-beneficiario a agregar.</param>
    public void AgregarBeneficiario(PolizaBeneficiario pb) => _beneficiarios.Add(pb);

    /// <summary>
    /// Agrega un <see cref="Pago"/> a la colección interna de la póliza.
    /// Utilizado en tests o durante la carga desde repositorio.
    /// </summary>
    /// <param name="pago">Pago a agregar a la póliza.</param>
    public void AgregarPago(Pago pago) => _pagos.Add(pago);
}
