namespace LinealyticsAPI.Models;

public class LecturaContador
{
    public int Id { get; set; }
    public int CorridaId { get; set; }
    public int MaquinaId { get; set; }
    public int ProductoId { get; set; }

    // Contador OK
    public long ContadorOK { get; set; }
    public long? ContadorOKAnterior { get; set; }
    public long DiferenciaOK { get; set; }
    public long ProduccionOK { get; set; }
    public bool EsResetOK { get; set; }

    // Contador NOK
    public long ContadorNOK { get; set; }
    public long? ContadorNOKAnterior { get; set; }
    public long DiferenciaNOK { get; set; }
    public long ProduccionNOK { get; set; }
    public bool EsResetNOK { get; set; }

    public DateTime FechaHoraLectura { get; set; } = DateTime.UtcNow;
}
