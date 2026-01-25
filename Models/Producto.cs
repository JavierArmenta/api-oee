namespace LinealyticsAPI.Models;

/// <summary>
/// Modelo de referencia (solo lectura) para validar que ProductoId existe.
/// La gestión de productos se hace desde webapp-oee.
/// </summary>
public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public bool Activo { get; set; } = true;
}
