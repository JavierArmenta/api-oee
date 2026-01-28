namespace LinealyticsAPI.Models;

/// <summary>
/// Modelo de referencia (solo lectura) para el catálogo de fallas.
/// La gestión del catálogo se hace desde webapp-oee.
/// </summary>
public class CatalogoFalla
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Severidad { get; set; }
    public string? Categoria { get; set; }
    public bool Activo { get; set; } = true;
}
