namespace LinealyticsAPI.DTOs;

// Request para insertar una falla (la fecha se calcula automáticamente)
public class InsertarFallaRequest
{
    public int FallaId { get; set; }
    public int MaquinaId { get; set; }
    public int? ModeloId { get; set; }
    public string? Descripcion { get; set; }
}

// Response para operación de insertar falla
public class FallaResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int FallaRegistroId { get; set; }
    public DateTime FechaHoraLectura { get; set; }
}

// DTO para listar fallas
public class FallaDto
{
    public int Id { get; set; }
    public int FallaId { get; set; }
    public int MaquinaId { get; set; }
    public DateTime FechaHoraLectura { get; set; }
    public int? ModeloId { get; set; }
    public string? Descripcion { get; set; }
}
