namespace LinealyticsAPI.Models;

public class ContadorDispositivo
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoContador { get; set; } = "Produccion"; // Produccion, Ciclos, Defectos
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
