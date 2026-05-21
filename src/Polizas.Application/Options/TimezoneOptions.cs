namespace Polizas.Application.Options;

/// <summary>
/// Opciones de configuración para la zona horaria utilizada en cálculos de mora.
/// Se enlaza desde la sección <c>Timezone</c> del archivo de configuración (appsettings.json).
/// </summary>
public sealed class TimezoneOptions
{
    /// <summary>Nombre de la sección en el archivo de configuración.</summary>
    public const string SectionName = "Timezone";

    /// <summary>
    /// ID de la zona horaria del sistema operativo.
    /// En Linux/macOS utilizar <c>"America/Bogota"</c> (IANA).
    /// En Windows utilizar <c>"SA Pacific Standard Time"</c>.
    /// El valor predeterminado es el identificador IANA para compatibilidad con contenedores Linux.
    /// </summary>
    public string TimeZoneId { get; set; } = "America/Bogota";
}
