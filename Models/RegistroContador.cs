namespace LinealyticsAPI.Models;

public class RegistroContador
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public int ContadorOK { get; set; }
    public int ContadorNOK { get; set; }
    public int? ModeloId { get; set; }
    public DateTime FechaHoraLectura { get; set; }
    public int TotalUnidades => ContadorOK + ContadorNOK;
    public decimal PorcentajeCalidad => TotalUnidades > 0 ? (decimal)ContadorOK / TotalUnidades * 100 : 0;
    public decimal PorcentajeDefectos => TotalUnidades > 0 ? (decimal)ContadorNOK / TotalUnidades * 100 : 0;
}
