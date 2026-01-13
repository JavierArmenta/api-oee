namespace LinealyticsAPI.Models;

public class RegistroParoBotonera
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public int DepartamentoId { get; set; }
    public int? OperadorId { get; set; }
    public int? BotonId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraFin { get; set; }
    public int? DuracionMinutos { get; set; }
    public string Estado { get; set; } = "Abierto"; // "Abierto" o "Cerrado"
}
