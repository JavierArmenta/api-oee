using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinealyticsAPI.Data;
using LinealyticsAPI.DTOs;
using LinealyticsAPI.Models;

namespace LinealyticsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContadoresProduccionController : ControllerBase
{
    private readonly LinealyticsDbContext _context;
    private readonly ILogger<ContadoresProduccionController> _logger;

    public ContadoresProduccionController(LinealyticsDbContext context, ILogger<ContadoresProduccionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Registra una lectura de contador. Maneja automáticamente:
    /// - Creación de corridas
    /// - Detección de cambios de producto
    /// - Detección de resets
    /// - Cálculo de producción incremental
    /// </summary>
    [HttpPost("lectura")]
    [ProducesResponseType(typeof(LecturaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LecturaResponse>> RegistrarLectura([FromBody] RegistrarLecturaRequest request)
    {
        try
        {
            // Validar CodigoMaquina
            if (string.IsNullOrWhiteSpace(request.CodigoMaquina))
            {
                return BadRequest(new LecturaResponse
                {
                    Exitoso = false,
                    Mensaje = "CodigoMaquina es requerido"
                });
            }

            // Buscar máquina por código
            var maquina = await _context.Maquinas
                .FirstOrDefaultAsync(m => m.Codigo == request.CodigoMaquina && m.Activo);

            if (maquina == null)
            {
                return BadRequest(new LecturaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró la máquina con código '{request.CodigoMaquina}' o está inactiva"
                });
            }

            var maquinaId = maquina.Id;

            // Buscar producto por código
            if (string.IsNullOrWhiteSpace(request.CodigoProducto))
            {
                return BadRequest(new LecturaResponse
                {
                    Exitoso = false,
                    Mensaje = "CodigoProducto es requerido"
                });
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Codigo == request.CodigoProducto && p.Activo);

            if (producto == null)
            {
                return BadRequest(new LecturaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró el producto con código '{request.CodigoProducto}' o está inactivo"
                });
            }

            var ahora = DateTime.UtcNow;

            // Buscar corrida activa para esta máquina
            var corridaActiva = await _context.CorridasProduccion
                .Where(c => c.MaquinaId == maquinaId && c.Estado == "Activa")
                .OrderByDescending(c => c.FechaInicio)
                .FirstOrDefaultAsync();

            bool nuevaCorridaCreada = false;
            bool corridaCerrada = false;
            long produccionOK = 0;
            long produccionNOK = 0;
            bool esResetOK = false;
            bool esResetNOK = false;
            long? contadorOKAnterior = null;
            long? contadorNOKAnterior = null;
            long diferenciaOK = 0;
            long diferenciaNOK = 0;

            // CASO 1: No hay corrida activa - crear nueva
            if (corridaActiva == null)
            {
                corridaActiva = new CorridaProduccion
                {
                    MaquinaId = maquinaId,
                    ProductoId = producto.Id,
                    FechaInicio = ahora,
                    ContadorOKInicial = request.OK,
                    ContadorOKFinal = request.OK,
                    UltimoContadorOK = request.OK,
                    ContadorNOKInicial = request.NOK,
                    ContadorNOKFinal = request.NOK,
                    UltimoContadorNOK = request.NOK,
                    ProduccionOK = 0,
                    ProduccionNOK = 0,
                    NumeroResetsOK = 0,
                    NumeroResetsNOK = 0,
                    NumeroLecturas = 0,
                    Estado = "Activa"
                };

                _context.CorridasProduccion.Add(corridaActiva);
                await _context.SaveChangesAsync();

                nuevaCorridaCreada = true;

                _logger.LogInformation(
                    "Nueva corrida {CorridaId} creada para máquina {MaquinaId}, producto {ProductoCodigo}, baseline OK={OK} NOK={NOK}",
                    corridaActiva.Id, maquinaId, request.CodigoProducto, request.OK, request.NOK);
            }
            // CASO 2: Cambió el producto - cerrar corrida actual y crear nueva
            else if (corridaActiva.ProductoId != producto.Id)
            {
                // Cerrar corrida actual
                corridaActiva.FechaFin = ahora;
                corridaActiva.Estado = "Cerrada";

                _logger.LogInformation(
                    "Corrida {CorridaId} cerrada por cambio de producto. Producción OK: {OK}, NOK: {NOK}",
                    corridaActiva.Id, corridaActiva.ProduccionOK, corridaActiva.ProduccionNOK);

                corridaCerrada = true;

                // Crear nueva corrida
                var nuevaCorrida = new CorridaProduccion
                {
                    MaquinaId = maquinaId,
                    ProductoId = producto.Id,
                    FechaInicio = ahora,
                    ContadorOKInicial = request.OK,
                    ContadorOKFinal = request.OK,
                    UltimoContadorOK = request.OK,
                    ContadorNOKInicial = request.NOK,
                    ContadorNOKFinal = request.NOK,
                    UltimoContadorNOK = request.NOK,
                    ProduccionOK = 0,
                    ProduccionNOK = 0,
                    NumeroResetsOK = 0,
                    NumeroResetsNOK = 0,
                    NumeroLecturas = 0,
                    Estado = "Activa"
                };

                _context.CorridasProduccion.Add(nuevaCorrida);
                await _context.SaveChangesAsync();

                corridaActiva = nuevaCorrida;
                nuevaCorridaCreada = true;

                _logger.LogInformation(
                    "Nueva corrida {CorridaId} creada para producto {ProductoCodigo}, baseline OK={OK} NOK={NOK}",
                    corridaActiva.Id, request.CodigoProducto, request.OK, request.NOK);
            }
            // CASO 3: Mismo producto - calcular incrementos
            else
            {
                contadorOKAnterior = corridaActiva.UltimoContadorOK;
                contadorNOKAnterior = corridaActiva.UltimoContadorNOK;
                diferenciaOK = request.OK - contadorOKAnterior.Value;
                diferenciaNOK = request.NOK - contadorNOKAnterior.Value;

                // Procesar contador OK
                if (diferenciaOK < 0)
                {
                    // Reset detectado en OK
                    esResetOK = true;
                    produccionOK = 0;
                    corridaActiva.NumeroResetsOK++;

                    _logger.LogInformation(
                        "RESET OK detectado en máquina {MaquinaId}: {Anterior} -> {Actual}",
                        maquinaId, contadorOKAnterior, request.OK);
                }
                else
                {
                    // Incremento normal OK
                    produccionOK = diferenciaOK;
                    corridaActiva.ProduccionOK += produccionOK;
                }

                // Procesar contador NOK
                if (diferenciaNOK < 0)
                {
                    // Reset detectado en NOK
                    esResetNOK = true;
                    produccionNOK = 0;
                    corridaActiva.NumeroResetsNOK++;

                    _logger.LogInformation(
                        "RESET NOK detectado en máquina {MaquinaId}: {Anterior} -> {Actual}",
                        maquinaId, contadorNOKAnterior, request.NOK);
                }
                else
                {
                    // Incremento normal NOK
                    produccionNOK = diferenciaNOK;
                    corridaActiva.ProduccionNOK += produccionNOK;
                }

                // Actualizar valores finales de la corrida
                corridaActiva.UltimoContadorOK = request.OK;
                corridaActiva.UltimoContadorNOK = request.NOK;
                corridaActiva.ContadorOKFinal = request.OK;
                corridaActiva.ContadorNOKFinal = request.NOK;
            }

            // Incrementar contador de lecturas
            corridaActiva.NumeroLecturas++;

            // Guardar lectura
            var lectura = new LecturaContador
            {
                CorridaId = corridaActiva.Id,
                MaquinaId = maquinaId,
                ProductoId = producto.Id,
                ContadorOK = request.OK,
                ContadorOKAnterior = contadorOKAnterior,
                DiferenciaOK = diferenciaOK,
                ProduccionOK = produccionOK,
                EsResetOK = esResetOK,
                ContadorNOK = request.NOK,
                ContadorNOKAnterior = contadorNOKAnterior,
                DiferenciaNOK = diferenciaNOK,
                ProduccionNOK = produccionNOK,
                EsResetNOK = esResetNOK,
                FechaHoraLectura = ahora
            };

            _context.LecturasContador.Add(lectura);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Lectura {LecturaId} registrada: OK={OK}(+{IncOK}), NOK={NOK}(+{IncNOK}), resetOK={ResetOK}, resetNOK={ResetNOK}",
                lectura.Id, request.OK, produccionOK, request.NOK, produccionNOK, esResetOK, esResetNOK);

            string mensaje = nuevaCorridaCreada ? "Lectura registrada (nueva corrida)" :
                             corridaCerrada ? "Lectura registrada (corrida cerrada, nueva iniciada)" :
                             (esResetOK || esResetNOK) ? "Lectura registrada (RESET detectado)" :
                             "Lectura registrada";

            return Ok(new LecturaResponse
            {
                Exitoso = true,
                Mensaje = mensaje,
                LecturaId = lectura.Id,
                CorridaId = corridaActiva.Id,
                ProduccionOK = produccionOK,
                ProduccionNOK = produccionNOK,
                ProduccionTotalCorridaOK = corridaActiva.ProduccionOK,
                ProduccionTotalCorridaNOK = corridaActiva.ProduccionNOK,
                EsResetOK = esResetOK,
                EsResetNOK = esResetNOK,
                NuevaCorridaCreada = nuevaCorridaCreada,
                CorridaCerrada = corridaCerrada
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar lectura para máquina {CodigoMaquina}", request.CodigoMaquina);
            return StatusCode(500, new LecturaResponse
            {
                Exitoso = false,
                Mensaje = "Error interno al registrar la lectura"
            });
        }
    }

    /// <summary>
    /// Obtiene las corridas de una máquina
    /// </summary>
    [HttpGet("{maquinaId}/corridas")]
    [ProducesResponseType(typeof(List<CorridaProduccionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CorridaProduccionDto>>> ObtenerCorridas(
        int maquinaId,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int limite = 50)
    {
        try
        {
            var query = _context.CorridasProduccion
                .Where(c => c.MaquinaId == maquinaId);

            if (desde.HasValue)
                query = query.Where(c => c.FechaInicio >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(c => c.FechaInicio <= hasta.Value);

            var corridas = await query
                .OrderByDescending(c => c.FechaInicio)
                .Take(limite)
                .Select(c => new CorridaProduccionDto
                {
                    Id = c.Id,
                    MaquinaId = c.MaquinaId,
                    ProductoId = c.ProductoId,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    ContadorOKInicial = c.ContadorOKInicial,
                    ContadorOKFinal = c.ContadorOKFinal,
                    ProduccionOK = c.ProduccionOK,
                    NumeroResetsOK = c.NumeroResetsOK,
                    ContadorNOKInicial = c.ContadorNOKInicial,
                    ContadorNOKFinal = c.ContadorNOKFinal,
                    ProduccionNOK = c.ProduccionNOK,
                    NumeroResetsNOK = c.NumeroResetsNOK,
                    NumeroLecturas = c.NumeroLecturas,
                    Estado = c.Estado,
                    DuracionMinutos = c.FechaFin.HasValue ?
                        (int)(c.FechaFin.Value - c.FechaInicio).TotalMinutes : null
                })
                .ToListAsync();

            return Ok(corridas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener corridas para máquina {MaquinaId}", maquinaId);
            return StatusCode(500, new List<CorridaProduccionDto>());
        }
    }

    /// <summary>
    /// Obtiene la corrida activa de una máquina
    /// </summary>
    [HttpGet("{maquinaId}/corrida-activa")]
    [ProducesResponseType(typeof(CorridaProduccionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CorridaProduccionDto>> ObtenerCorridaActiva(int maquinaId)
    {
        try
        {
            var corrida = await _context.CorridasProduccion
                .Where(c => c.MaquinaId == maquinaId && c.Estado == "Activa")
                .OrderByDescending(c => c.FechaInicio)
                .Select(c => new CorridaProduccionDto
                {
                    Id = c.Id,
                    MaquinaId = c.MaquinaId,
                    ProductoId = c.ProductoId,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    ContadorOKInicial = c.ContadorOKInicial,
                    ContadorOKFinal = c.ContadorOKFinal,
                    ProduccionOK = c.ProduccionOK,
                    NumeroResetsOK = c.NumeroResetsOK,
                    ContadorNOKInicial = c.ContadorNOKInicial,
                    ContadorNOKFinal = c.ContadorNOKFinal,
                    ProduccionNOK = c.ProduccionNOK,
                    NumeroResetsNOK = c.NumeroResetsNOK,
                    NumeroLecturas = c.NumeroLecturas,
                    Estado = c.Estado
                })
                .FirstOrDefaultAsync();

            if (corrida == null)
            {
                return NotFound(new { mensaje = "No hay corrida activa para esta máquina" });
            }

            return Ok(corrida);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener corrida activa para máquina {MaquinaId}", maquinaId);
            return StatusCode(500, new { mensaje = "Error interno" });
        }
    }

    /// <summary>
    /// Obtiene las lecturas de una corrida específica
    /// </summary>
    [HttpGet("corridas/{corridaId}/lecturas")]
    [ProducesResponseType(typeof(List<LecturaContadorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LecturaContadorDto>>> ObtenerLecturasCorrida(int corridaId, [FromQuery] int limite = 1000)
    {
        try
        {
            var lecturas = await _context.LecturasContador
                .Where(l => l.CorridaId == corridaId)
                .OrderBy(l => l.FechaHoraLectura)
                .Take(limite)
                .Select(l => new LecturaContadorDto
                {
                    Id = l.Id,
                    CorridaId = l.CorridaId,
                    MaquinaId = l.MaquinaId,
                    ProductoId = l.ProductoId,
                    ContadorOK = l.ContadorOK,
                    ContadorOKAnterior = l.ContadorOKAnterior,
                    DiferenciaOK = l.DiferenciaOK,
                    ProduccionOK = l.ProduccionOK,
                    EsResetOK = l.EsResetOK,
                    ContadorNOK = l.ContadorNOK,
                    ContadorNOKAnterior = l.ContadorNOKAnterior,
                    DiferenciaNOK = l.DiferenciaNOK,
                    ProduccionNOK = l.ProduccionNOK,
                    EsResetNOK = l.EsResetNOK,
                    FechaHoraLectura = l.FechaHoraLectura
                })
                .ToListAsync();

            return Ok(lecturas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lecturas para corrida {CorridaId}", corridaId);
            return StatusCode(500, new List<LecturaContadorDto>());
        }
    }

    /// <summary>
    /// Obtiene histórico de lecturas para gráficas
    /// </summary>
    [HttpGet("{maquinaId}/historico")]
    [ProducesResponseType(typeof(HistoricoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HistoricoResponse>> ObtenerHistorico(
        int maquinaId,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int limite = 1000)
    {
        try
        {
            var fechaHasta = hasta ?? DateTime.UtcNow;
            var fechaDesde = desde ?? fechaHasta.AddDays(-7);

            var maquina = await _context.Maquinas
                .Where(m => m.Id == maquinaId)
                .Select(m => new { m.Id, m.Nombre })
                .FirstOrDefaultAsync();

            if (maquina == null)
            {
                return NotFound(new { mensaje = "Máquina no encontrada" });
            }

            var lecturas = await _context.LecturasContador
                .Where(l => l.MaquinaId == maquinaId
                         && l.FechaHoraLectura >= fechaDesde
                         && l.FechaHoraLectura <= fechaHasta)
                .OrderBy(l => l.FechaHoraLectura)
                .Take(limite)
                .Select(l => new LecturaContadorDto
                {
                    Id = l.Id,
                    CorridaId = l.CorridaId,
                    MaquinaId = l.MaquinaId,
                    ProductoId = l.ProductoId,
                    ContadorOK = l.ContadorOK,
                    ContadorOKAnterior = l.ContadorOKAnterior,
                    DiferenciaOK = l.DiferenciaOK,
                    ProduccionOK = l.ProduccionOK,
                    EsResetOK = l.EsResetOK,
                    ContadorNOK = l.ContadorNOK,
                    ContadorNOKAnterior = l.ContadorNOKAnterior,
                    DiferenciaNOK = l.DiferenciaNOK,
                    ProduccionNOK = l.ProduccionNOK,
                    EsResetNOK = l.EsResetNOK,
                    FechaHoraLectura = l.FechaHoraLectura
                })
                .ToListAsync();

            return Ok(new HistoricoResponse
            {
                MaquinaId = maquinaId,
                MaquinaNombre = maquina.Nombre,
                Desde = fechaDesde,
                Hasta = fechaHasta,
                Lecturas = lecturas,
                TotalProduccionOK = lecturas.Sum(l => l.ProduccionOK),
                TotalProduccionNOK = lecturas.Sum(l => l.ProduccionNOK),
                TotalResetsOK = lecturas.Count(l => l.EsResetOK),
                TotalResetsNOK = lecturas.Count(l => l.EsResetNOK)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener histórico para máquina {MaquinaId}", maquinaId);
            return StatusCode(500, new { mensaje = "Error interno" });
        }
    }

    /// <summary>
    /// Obtiene lecturas en tiempo real (últimos N minutos)
    /// </summary>
    [HttpGet("{maquinaId}/lecturas")]
    [ProducesResponseType(typeof(LecturasRealtimeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LecturasRealtimeResponse>> ObtenerLecturasRealtime(
        int maquinaId,
        [FromQuery] int ultimosMinutos = 120)
    {
        try
        {
            var desde = DateTime.UtcNow.AddMinutes(-ultimosMinutos);

            var maquina = await _context.Maquinas
                .Where(m => m.Id == maquinaId)
                .Select(m => new { m.Id, m.Nombre })
                .FirstOrDefaultAsync();

            if (maquina == null)
            {
                return NotFound(new { mensaje = "Máquina no encontrada" });
            }

            var corridaActiva = await _context.CorridasProduccion
                .Where(c => c.MaquinaId == maquinaId && c.Estado == "Activa")
                .Select(c => new CorridaProduccionDto
                {
                    Id = c.Id,
                    MaquinaId = c.MaquinaId,
                    ProductoId = c.ProductoId,
                    FechaInicio = c.FechaInicio,
                    ContadorOKInicial = c.ContadorOKInicial,
                    ContadorOKFinal = c.ContadorOKFinal,
                    ProduccionOK = c.ProduccionOK,
                    NumeroResetsOK = c.NumeroResetsOK,
                    ContadorNOKInicial = c.ContadorNOKInicial,
                    ContadorNOKFinal = c.ContadorNOKFinal,
                    ProduccionNOK = c.ProduccionNOK,
                    NumeroResetsNOK = c.NumeroResetsNOK,
                    NumeroLecturas = c.NumeroLecturas,
                    Estado = c.Estado
                })
                .FirstOrDefaultAsync();

            var lecturas = await _context.LecturasContador
                .Where(l => l.MaquinaId == maquinaId && l.FechaHoraLectura >= desde)
                .OrderBy(l => l.FechaHoraLectura)
                .Select(l => new PuntoLectura
                {
                    FechaHora = l.FechaHoraLectura,
                    ContadorOK = l.ContadorOK,
                    ContadorNOK = l.ContadorNOK,
                    ProduccionOK = l.ProduccionOK,
                    ProduccionNOK = l.ProduccionNOK,
                    EsResetOK = l.EsResetOK,
                    EsResetNOK = l.EsResetNOK
                })
                .ToListAsync();

            return Ok(new LecturasRealtimeResponse
            {
                MaquinaId = maquinaId,
                MaquinaNombre = maquina.Nombre,
                CorridaActiva = corridaActiva,
                Lecturas = lecturas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lecturas realtime para máquina {MaquinaId}", maquinaId);
            return StatusCode(500, new { mensaje = "Error interno" });
        }
    }
}
