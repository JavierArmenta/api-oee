namespace LinealyticsAPI.Models;

public class Operador
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NumeroEmpleado { get; set; } = string.Empty;
    public string CodigoPinHashed { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public bool Activo { get; set; } = true;
}
