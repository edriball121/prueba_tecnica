namespace Polizas.Domain.Enums;

/// <summary>
/// Estado de cartera de una póliza respecto al cumplimiento de pagos.
/// </summary>
public enum EstadoCartera
{
    /// <summary>El tomador está al día: pagó el total o la fecha de vencimiento no ha pasado.</summary>
    AlDia,

    /// <summary>
    /// El tomador está en mora: la fecha de vencimiento ya pasó y el total pagado
    /// es menor a la prima total.
    /// </summary>
    EnMora
}
