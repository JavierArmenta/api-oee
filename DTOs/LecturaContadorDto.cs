namespace LinealyticsAPI.DTOs;

// Request para insertar una lectura
public class InsertarLecturaRequest
{
    public int ContadorDispositivoId { get; set; }
    public int? ProductoId { get; set; }
    public long Valor { get; set; }
}

// Response de insertar lectura
public class LecturaResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? LecturaId { get; set; }
    public int? CorridaId { get; set; }
    public long ProduccionIncremental { get; set; }
    public long ProduccionTotalCorrida { get; set; }
    public bool EsReset { get; set; }
    public bool EsRuido { get; set; }
    public bool NuevaCorridaCreada { get; set; }
    public bool CorridaCerrada { get; set; }
}

// DTO para lectura individual
public class LecturaContadorDto
{
    public int Id { get; set; }
    public int CorridaId { get; set; }
    public int ContadorDispositivoId { get; set; }
    public int? ProductoId { get; set; }
    public long ContadorValor { get; set; }
    public long? ContadorAnterior { get; set; }
    public long Diferencia { get; set; }
    public long ProduccionIncremental { get; set; }
    public bool EsReset { get; set; }
    public bool EsRuido { get; set; }
    public DateTime FechaHoraLectura { get; set; }
}

// DTO para corrida
public class CorridaProduccionDto
{
    public int Id { get; set; }
    public int ContadorDispositivoId { get; set; }
    public string? ContadorNombre { get; set; }
    public int? ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public long ContadorInicial { get; set; }
    public long ContadorFinal { get; set; }
    public long ProduccionTotal { get; set; }
    public int NumeroResets { get; set; }
    public int NumeroLecturas { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? DuracionMinutos { get; set; }
}

// Request para histórico
public class HistoricoRequest
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string Granularidad { get; set; } = "hora"; // hora, dia
}

// Response para histórico (gráficas)
public class HistoricoResponse
{
    public int ContadorDispositivoId { get; set; }
    public string ContadorNombre { get; set; } = string.Empty;
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string Granularidad { get; set; } = string.Empty;
    public List<DatoHistorico> Datos { get; set; } = new();
    public long TotalProduccion { get; set; }
    public int TotalResets { get; set; }
}

public class DatoHistorico
{
    public DateTime Periodo { get; set; }
    public long Produccion { get; set; }
    public int Resets { get; set; }
    public int Lecturas { get; set; }
    public long? ContadorInicio { get; set; }
    public long? ContadorFin { get; set; }
}

// Response para lecturas en tiempo real
public class LecturasRealtimeResponse
{
    public int ContadorDispositivoId { get; set; }
    public string ContadorNombre { get; set; } = string.Empty;
    public List<PuntoLectura> Lecturas { get; set; } = new();
}

public class PuntoLectura
{
    public DateTime FechaHora { get; set; }
    public long Valor { get; set; }
    public long ProduccionIncremental { get; set; }
    public bool EsReset { get; set; }
}

// DTO para contador dispositivo
public class ContadorDispositivoDto
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public string? MaquinaNombre { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoContador { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public CorridaProduccionDto? CorridaActiva { get; set; }
}
