namespace LinealyticsAPI.Models;

public class CorridaProduccion
{
    public int Id { get; set; }
    public int ContadorDispositivoId { get; set; }
    public int? ProductoId { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public long ContadorInicial { get; set; }
    public long ContadorFinal { get; set; }
    public long UltimoContadorValor { get; set; }
    public long ProduccionTotal { get; set; }
    public int NumeroResets { get; set; }
    public int NumeroLecturas { get; set; }
    public string Estado { get; set; } = "Activa"; // Activa, Cerrada
}
