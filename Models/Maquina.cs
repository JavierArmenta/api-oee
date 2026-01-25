namespace LinealyticsAPI.Models;

/// <summary>
/// Modelo de referencia (solo lectura) para validar que MaquinaId existe.
/// La gestión de máquinas se hace desde webapp-oee.
/// </summary>
public class Maquina
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
