namespace Polizas.Domain.Enums;

/// <summary>
/// Estado de vigencia de una póliza de seguro.
/// </summary>
public enum EstadoPoliza
{
    /// <summary>La póliza está vigente y en curso.</summary>
    Activa,

    /// <summary>La póliza ha superado su fecha de vencimiento.</summary>
    Vencida,

    /// <summary>La póliza fue cancelada antes de su vencimiento.</summary>
    Cancelada
}
