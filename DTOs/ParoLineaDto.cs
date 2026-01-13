namespace LinealyticsAPI.DTOs;

// Request para registrar paro de línea (abre o cierra automáticamente)
public class RegistrarParoLineaRequest
{
    public string Botonera { get; set; } = string.Empty;  // Número de serie de la botonera (ej: "BTNR-1")
    public string Boton { get; set; } = string.Empty;      // Código del botón (ej: "BTN-1")
    public int? OperadorId { get; set; }                   // Opcional. Si es 0, se guarda como null
}

// Response para operaciones de paro de línea
public class ParoLineaResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int ParoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? DuracionMinutos { get; set; }
}

// DTO para listar paros abiertos
public class ParoLineaDto
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public int DepartamentoId { get; set; }
    public int? OperadorId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraFin { get; set; }
    public int? DuracionMinutos { get; set; }
    public string Estado { get; set; } = string.Empty;
}
