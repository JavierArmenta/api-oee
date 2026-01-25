namespace LinealyticsAPI.Models;

public class ResumenProduccionDia
{
    public int Id { get; set; }
    public int ContadorDispositivoId { get; set; }
    public int? ProductoId { get; set; }
    public DateOnly Fecha { get; set; }
    public long ProduccionTotal { get; set; }
    public int NumeroLecturas { get; set; }
    public int NumeroResets { get; set; }
    public int NumeroCorridasIniciadas { get; set; }
    public int NumeroCorridasCerradas { get; set; }
    public int TiempoProduccionMinutos { get; set; }
}
