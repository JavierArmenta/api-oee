namespace LinealyticsAPI.DTOs;

// Request para insertar una falla por IDs
public class InsertarFallaRequest
{
    public int CatalogoFallaId { get; set; }
    public int MaquinaId { get; set; }
}

// Request para insertar una falla usando códigos en lugar de IDs
public class InsertarFallaPorCodigoRequest
{
    public string CodigoMaquina { get; set; } = string.Empty;
    public string CodigoFalla { get; set; } = string.Empty;
}

// Response para operación de insertar falla
public class FallaResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int FallaRegistroId { get; set; }
    public DateTime FechaHoraDeteccion { get; set; }
}

// DTO para listar fallas
public class FallaDto
{
    public int Id { get; set; }
    public int CatalogoFallaId { get; set; }
    public int MaquinaId { get; set; }
    public DateTime FechaHoraDeteccion { get; set; }
    public string Estado { get; set; } = string.Empty;
}
