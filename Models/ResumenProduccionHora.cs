namespace LinealyticsAPI.Models;

public class ResumenProduccionHora
{
    public int Id { get; set; }
    public int ContadorDispositivoId { get; set; }
    public int? ProductoId { get; set; }
    public DateOnly Fecha { get; set; }
    public int Hora { get; set; } // 0-23
    public long ProduccionTotal { get; set; }
    public int NumeroLecturas { get; set; }
    public int NumeroResets { get; set; }
    public long ContadorInicio { get; set; }
    public long ContadorFin { get; set; }
    public long ValorMinimo { get; set; }
    public long ValorMaximo { get; set; }
}
