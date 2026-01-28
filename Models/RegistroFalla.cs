namespace LinealyticsAPI.Models;

/// <summary>
/// Registro de falla detectada - Debe coincidir con la tabla en webapp-oee
/// </summary>
public class RegistroFalla
{
    public int Id { get; set; }
    public int CatalogoFallaId { get; set; }
    public int MaquinaId { get; set; }
    public DateTime FechaHoraDeteccion { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
