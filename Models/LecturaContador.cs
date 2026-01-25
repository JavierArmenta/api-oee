namespace LinealyticsAPI.Models;

public class LecturaContador
{
    public int Id { get; set; }
    public int CorridaId { get; set; }
    public int ContadorDispositivoId { get; set; }
    public int? ProductoId { get; set; }
    public long ContadorValor { get; set; }
    public long? ContadorAnterior { get; set; }
    public long Diferencia { get; set; }
    public long ProduccionIncremental { get; set; }
    public bool EsReset { get; set; }
    public bool EsRuido { get; set; }
    public DateTime FechaHoraLectura { get; set; } = DateTime.UtcNow;
}
