namespace LinealyticsAPI.DTOs;

// Request para insertar un contador (la fecha se calcula automáticamente)
public class InsertarContadorRequest
{
    public int MaquinaId { get; set; }
    public int ContadorOK { get; set; }
    public int ContadorNOK { get; set; }
    public int? ModeloId { get; set; }
}

// Response para operación de insertar contador
public class ContadorResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int ContadorId { get; set; }
    public int TotalUnidades { get; set; }
    public decimal PorcentajeCalidad { get; set; }
    public decimal PorcentajeDefectos { get; set; }
}

// DTO para listar contadores
public class ContadorDto
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public int ContadorOK { get; set; }
    public int ContadorNOK { get; set; }
    public DateTime FechaHoraLectura { get; set; }
    public int? ModeloId { get; set; }
}
