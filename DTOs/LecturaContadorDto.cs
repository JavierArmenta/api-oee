namespace LinealyticsAPI.DTOs;

// Request para registrar una lectura
public class RegistrarLecturaRequest
{
    public int MaquinaId { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public long OK { get; set; }
    public long NOK { get; set; }
}

// Response de registrar lectura
public class LecturaResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? LecturaId { get; set; }
    public int? CorridaId { get; set; }
    public long ProduccionOK { get; set; }
    public long ProduccionNOK { get; set; }
    public long ProduccionTotalCorridaOK { get; set; }
    public long ProduccionTotalCorridaNOK { get; set; }
    public bool EsResetOK { get; set; }
    public bool EsResetNOK { get; set; }
    public bool NuevaCorridaCreada { get; set; }
    public bool CorridaCerrada { get; set; }
}

// DTO para lectura individual
public class LecturaContadorDto
{
    public int Id { get; set; }
    public int CorridaId { get; set; }
    public int MaquinaId { get; set; }
    public int ProductoId { get; set; }

    // Contador OK
    public long ContadorOK { get; set; }
    public long? ContadorOKAnterior { get; set; }
    public long DiferenciaOK { get; set; }
    public long ProduccionOK { get; set; }
    public bool EsResetOK { get; set; }

    // Contador NOK
    public long ContadorNOK { get; set; }
    public long? ContadorNOKAnterior { get; set; }
    public long DiferenciaNOK { get; set; }
    public long ProduccionNOK { get; set; }
    public bool EsResetNOK { get; set; }

    public DateTime FechaHoraLectura { get; set; }
}

// DTO para corrida
public class CorridaProduccionDto
{
    public int Id { get; set; }
    public int MaquinaId { get; set; }
    public string? MaquinaNombre { get; set; }
    public int ProductoId { get; set; }
    public string? ProductoCodigo { get; set; }
    public string? ProductoNombre { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // Contadores OK
    public long ContadorOKInicial { get; set; }
    public long ContadorOKFinal { get; set; }
    public long ProduccionOK { get; set; }
    public int NumeroResetsOK { get; set; }

    // Contadores NOK
    public long ContadorNOKInicial { get; set; }
    public long ContadorNOKFinal { get; set; }
    public long ProduccionNOK { get; set; }
    public int NumeroResetsNOK { get; set; }

    public int NumeroLecturas { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? DuracionMinutos { get; set; }
}

// Response para histórico (gráficas)
public class HistoricoResponse
{
    public int MaquinaId { get; set; }
    public string MaquinaNombre { get; set; } = string.Empty;
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public List<LecturaContadorDto> Lecturas { get; set; } = new();
    public long TotalProduccionOK { get; set; }
    public long TotalProduccionNOK { get; set; }
    public int TotalResetsOK { get; set; }
    public int TotalResetsNOK { get; set; }
}

// Response para lecturas en tiempo real
public class LecturasRealtimeResponse
{
    public int MaquinaId { get; set; }
    public string MaquinaNombre { get; set; } = string.Empty;
    public CorridaProduccionDto? CorridaActiva { get; set; }
    public List<PuntoLectura> Lecturas { get; set; } = new();
}

public class PuntoLectura
{
    public DateTime FechaHora { get; set; }
    public long ContadorOK { get; set; }
    public long ContadorNOK { get; set; }
    public long ProduccionOK { get; set; }
    public long ProduccionNOK { get; set; }
    public bool EsResetOK { get; set; }
    public bool EsResetNOK { get; set; }
}
