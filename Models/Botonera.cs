namespace LinealyticsAPI.Models;

public class Botonera
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string DireccionIP { get; set; } = string.Empty;
    public string? NumeroSerie { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public int MaquinaId { get; set; }
}
