namespace LinealyticsAPI.Models;

public class Boton
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public int DepartamentoOperadorId { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaUltimaActivacion { get; set; }
}
