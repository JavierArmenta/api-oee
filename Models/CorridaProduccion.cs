namespace LinealyticsAPI.Models;

public class CorridaProduccion
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public int ProductoId { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }

    // Contadores OK
    public long ContadorOKInicial { get; set; }
    public long ContadorOKFinal { get; set; }
    public long UltimoContadorOK { get; set; }
    public long ProduccionOK { get; set; }
    public int NumeroResetsOK { get; set; }

    // Contadores NOK
    public long ContadorNOKInicial { get; set; }
    public long ContadorNOKFinal { get; set; }
    public long UltimoContadorNOK { get; set; }
    public long ProduccionNOK { get; set; }
    public int NumeroResetsNOK { get; set; }

    // Metadatos
    public int NumeroLecturas { get; set; }
    public string Estado { get; set; } = "Activa"; // Activa, Cerrada
}
