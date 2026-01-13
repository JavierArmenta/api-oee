namespace LinealyticsAPI.Models;

public class RegistroFalla
{
    public int Id { get; set; }
    public int FallaId { get; set; }
    public int MaquinaId { get; set; }
    public int? ModeloId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaHoraLectura { get; set; }
}
