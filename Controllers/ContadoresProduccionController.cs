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
    private const int THRESHOLD_RUIDO = 5;

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
    /// - Actualización de resúmenes
    /// </summary>
    [HttpPost("lectura")]
    [ProducesResponseType(typeof(LecturaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LecturaResponse>> RegistrarLectura([FromBody] InsertarLecturaRequest request)
    {
        try
        {
            // Validar contador dispositivo
            if (request.ContadorDispositivoId <= 0)
            {
                return BadRequest(new LecturaResponse
                {
                    Exitoso = false,
                    Mensaje = "ContadorDispositivoId debe ser mayor a 0"
                });
            }

            // Verificar que el contador existe
            var contadorExiste = await _context.ContadoresDispositivo
                .AnyAsync(c => c.Id == request.ContadorDispositivoId && c.Activo);

            if (!contadorExiste)
            {
                return BadRequest(new LecturaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró el contador con ID {request.ContadorDispositivoId} o está inactivo"
                });
            }

            // Validar que el producto existe si se proporciona
            if (request.ProductoId.HasValue)
            {
                var productoExiste = await _context.Productos
                    .AnyAsync(p => p.Id == request.ProductoId.Value && p.Activo);

                if (!productoExiste)
                {
                    return BadRequest(new LecturaResponse
                    {
                        Exitoso = false,
                        Mensaje = $"No se encontró el producto con ID {request.ProductoId.Value} o está inactivo"
                    });
                }
            }

            var ahora = DateTime.UtcNow;
            var fechaHoy = DateOnly.FromDateTime(ahora);
            var horaActual = ahora.Hour;

            // Buscar corrida activa para este contador
            var corridaActiva = await _context.CorridasProduccion
                .Where(c => c.ContadorDispositivoId == request.ContadorDispositivoId && c.Estado == "Activa")
                .OrderByDescending(c => c.FechaInicio)
                .FirstOrDefaultAsync();

            bool nuevaCorridaCreada = false;
            bool corridaCerrada = false;
            long produccionIncremental = 0;
            bool esReset = false;
            bool esRuido = false;
            long? contadorAnterior = null;
            long diferencia = 0;

            // CASO 1: No hay corrida activa - crear nueva
            if (corridaActiva == null)
            {
                corridaActiva = new CorridaProduccion
                {
                    ContadorDispositivoId = request.ContadorDispositivoId,
                    ProductoId = request.ProductoId,
                    FechaInicio = ahora,
                    ContadorInicial = request.Valor,
                    ContadorFinal = request.Valor,
                    UltimoContadorValor = request.Valor,
                    ProduccionTotal = 0,
                    NumeroResets = 0,
                    NumeroLecturas = 0,
                    Estado = "Activa"
                };

                _context.CorridasProduccion.Add(corridaActiva);
                await _context.SaveChangesAsync();

                nuevaCorridaCreada = true;
                produccionIncremental = 0;

                _logger.LogInformation("Nueva corrida {CorridaId} creada para contador {ContadorId}, producto {ProductoId}, baseline={Valor}",
                    corridaActiva.Id, request.ContadorDispositivoId, request.ProductoId, request.Valor);
            }
            // CASO 2: Cambió el producto - cerrar corrida actual y crear nueva
            else if (corridaActiva.ProductoId != request.ProductoId)
            {
                // Cerrar corrida actual
                corridaActiva.FechaFin = ahora;
                corridaActiva.Estado = "Cerrada";

                _logger.LogInformation("Corrida {CorridaId} cerrada por cambio de producto. Producción total: {Produccion}",
                    corridaActiva.Id, corridaActiva.ProduccionTotal);

                corridaCerrada = true;

                // Crear nueva corrida
                var nuevaCorrida = new CorridaProduccion
                {
                    ContadorDispositivoId = request.ContadorDispositivoId,
                    ProductoId = request.ProductoId,
                    FechaInicio = ahora,
                    ContadorInicial = request.Valor,
                    ContadorFinal = request.Valor,
                    UltimoContadorValor = request.Valor,
                    ProduccionTotal = 0,
                    NumeroResets = 0,
                    NumeroLecturas = 0,
                    Estado = "Activa"
                };

                _context.CorridasProduccion.Add(nuevaCorrida);
                await _context.SaveChangesAsync();

                corridaActiva = nuevaCorrida;
                nuevaCorridaCreada = true;
                produccionIncremental = 0;

                _logger.LogInformation("Nueva corrida {CorridaId} creada para producto {ProductoId}, baseline={Valor}",
                    corridaActiva.Id, request.ProductoId, request.Valor);
            }
            // CASO 3: Mismo producto - calcular incremento
            else
            {
                contadorAnterior = corridaActiva.UltimoContadorValor;
                diferencia = request.Valor - contadorAnterior.Value;

                if (diferencia < 0)
                {
                    // Decremento detectado
                    if (Math.Abs(diferencia) <= THRESHOLD_RUIDO)
                    {
                        // Es ruido - ignorar
                        esRuido = true;
                        produccionIncremental = 0;

                        _logger.LogDebug("Ruido detectado en contador {ContadorId}: diferencia={Diferencia}",
                            request.ContadorDispositivoId, diferencia);
                    }
                    else
                    {
                        // Es reset
                        esReset = true;
                        produccionIncremental = 0;
                        corridaActiva.NumeroResets++;

                        _logger.LogInformation("RESET detectado en contador {ContadorId}: {Anterior} -> {Actual}",
                            request.ContadorDispositivoId, contadorAnterior, request.Valor);
                    }
                }
                else
                {
                    // Incremento normal
                    produccionIncremental = diferencia;
                    corridaActiva.ProduccionTotal += produccionIncremental;
                }

                // Actualizar corrida
                corridaActiva.UltimoContadorValor = request.Valor;
                corridaActiva.ContadorFinal = request.Valor;
            }

            // Incrementar contador de lecturas
            corridaActiva.NumeroLecturas++;

            // Guardar lectura
            var lectura = new LecturaContador
            {
                CorridaId = corridaActiva.Id,
                ContadorDispositivoId = request.ContadorDispositivoId,
                ProductoId = request.ProductoId,
                ContadorValor = request.Valor,
                ContadorAnterior = contadorAnterior,
                Diferencia = diferencia,
                ProduccionIncremental = produccionIncremental,
                EsReset = esReset,
                EsRuido = esRuido,
                FechaHoraLectura = ahora
            };

            _context.LecturasContador.Add(lectura);

            // Actualizar resumen por hora
            await ActualizarResumenHora(request.ContadorDispositivoId, request.ProductoId, fechaHoy, horaActual,
                produccionIncremental, esReset, request.Valor);

            // Actualizar resumen por día
            await ActualizarResumenDia(request.ContadorDispositivoId, request.ProductoId, fechaHoy,
                produccionIncremental, esReset, nuevaCorridaCreada, corridaCerrada);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Lectura {LecturaId} registrada: valor={Valor}, incremental={Incremental}, reset={Reset}, ruido={Ruido}",
                lectura.Id, request.Valor, produccionIncremental, esReset, esRuido);

            return Ok(new LecturaResponse
            {
                Exitoso = true,
                Mensaje = esReset ? "Lectura registrada (RESET detectado)" :
                          esRuido ? "Lectura registrada (ruido ignorado)" :
                          nuevaCorridaCreada ? "Lectura registrada (nueva corrida)" :
                          "Lectura registrada",
                LecturaId = lectura.Id,
                CorridaId = corridaActiva.Id,
                ProduccionIncremental = produccionIncremental,
                ProduccionTotalCorrida = corridaActiva.ProduccionTotal,
                EsReset = esReset,
                EsRuido = esRuido,
                NuevaCorridaCreada = nuevaCorridaCreada,
                CorridaCerrada = corridaCerrada
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar lectura para contador {ContadorId}", request.ContadorDispositivoId);
            return StatusCode(500, new LecturaResponse
            {
                Exitoso = false,
                Mensaje = "Error interno al registrar la lectura"
            });
        }
    }

    private async Task ActualizarResumenHora(int contadorId, int? productoId, DateOnly fecha, int hora,
        long produccion, bool esReset, long valorContador)
    {
        var resumen = await _context.ResumenesProduccionHora
            .FirstOrDefaultAsync(r => r.ContadorDispositivoId == contadorId && r.Fecha == fecha && r.Hora == hora);

        if (resumen == null)
        {
            resumen = new ResumenProduccionHora
            {
                ContadorDispositivoId = contadorId,
                ProductoId = productoId,
                Fecha = fecha,
                Hora = hora,
                ProduccionTotal = produccion,
                NumeroLecturas = 1,
                NumeroResets = esReset ? 1 : 0,
                ContadorInicio = valorContador,
                ContadorFin = valorContador,
                ValorMinimo = valorContador,
                ValorMaximo = valorContador
            };
            _context.ResumenesProduccionHora.Add(resumen);
        }
        else
        {
            resumen.ProduccionTotal += produccion;
            resumen.NumeroLecturas++;
            if (esReset) resumen.NumeroResets++;
            resumen.ContadorFin = valorContador;
            if (valorContador < resumen.ValorMinimo) resumen.ValorMinimo = valorContador;
            if (valorContador > resumen.ValorMaximo) resumen.ValorMaximo = valorContador;
        }
    }

    private async Task ActualizarResumenDia(int contadorId, int? productoId, DateOnly fecha,
        long produccion, bool esReset, bool corridaIniciada, bool corridaCerrada)
    {
        var resumen = await _context.ResumenesProduccionDia
            .FirstOrDefaultAsync(r => r.ContadorDispositivoId == contadorId && r.Fecha == fecha);

        if (resumen == null)
        {
            resumen = new ResumenProduccionDia
            {
                ContadorDispositivoId = contadorId,
                ProductoId = productoId,
                Fecha = fecha,
                ProduccionTotal = produccion,
                NumeroLecturas = 1,
                NumeroResets = esReset ? 1 : 0,
                NumeroCorridasIniciadas = corridaIniciada ? 1 : 0,
                NumeroCorridasCerradas = corridaCerrada ? 1 : 0,
                TiempoProduccionMinutos = 0
            };
            _context.ResumenesProduccionDia.Add(resumen);
        }
        else
        {
            resumen.ProduccionTotal += produccion;
            resumen.NumeroLecturas++;
            if (esReset) resumen.NumeroResets++;
            if (corridaIniciada) resumen.NumeroCorridasIniciadas++;
            if (corridaCerrada) resumen.NumeroCorridasCerradas++;
        }
    }

    /// <summary>
    /// Obtiene las corridas de un contador
    /// </summary>
    [HttpGet("{contadorId}/corridas")]
    [ProducesResponseType(typeof(List<CorridaProduccionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CorridaProduccionDto>>> ObtenerCorridas(
        int contadorId,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int limite = 50)
    {
        try
        {
            var query = _context.CorridasProduccion
                .Where(c => c.ContadorDispositivoId == contadorId);

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
                    ContadorDispositivoId = c.ContadorDispositivoId,
                    ProductoId = c.ProductoId,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    ContadorInicial = c.ContadorInicial,
                    ContadorFinal = c.ContadorFinal,
                    ProduccionTotal = c.ProduccionTotal,
                    NumeroResets = c.NumeroResets,
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
            _logger.LogError(ex, "Error al obtener corridas para contador {ContadorId}", contadorId);
            return StatusCode(500, new List<CorridaProduccionDto>());
        }
    }

    /// <summary>
    /// Obtiene la corrida activa de un contador
    /// </summary>
    [HttpGet("{contadorId}/corrida-activa")]
    [ProducesResponseType(typeof(CorridaProduccionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CorridaProduccionDto>> ObtenerCorridaActiva(int contadorId)
    {
        try
        {
            var corrida = await _context.CorridasProduccion
                .Where(c => c.ContadorDispositivoId == contadorId && c.Estado == "Activa")
                .OrderByDescending(c => c.FechaInicio)
                .Select(c => new CorridaProduccionDto
                {
                    Id = c.Id,
                    ContadorDispositivoId = c.ContadorDispositivoId,
                    ProductoId = c.ProductoId,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    ContadorInicial = c.ContadorInicial,
                    ContadorFinal = c.ContadorFinal,
                    ProduccionTotal = c.ProduccionTotal,
                    NumeroResets = c.NumeroResets,
                    NumeroLecturas = c.NumeroLecturas,
                    Estado = c.Estado
                })
                .FirstOrDefaultAsync();

            if (corrida == null)
            {
                return NotFound(new { mensaje = "No hay corrida activa para este contador" });
            }

            return Ok(corrida);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener corrida activa para contador {ContadorId}", contadorId);
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
                    ContadorDispositivoId = l.ContadorDispositivoId,
                    ProductoId = l.ProductoId,
                    ContadorValor = l.ContadorValor,
                    ContadorAnterior = l.ContadorAnterior,
                    Diferencia = l.Diferencia,
                    ProduccionIncremental = l.ProduccionIncremental,
                    EsReset = l.EsReset,
                    EsRuido = l.EsRuido,
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
    /// Obtiene histórico de producción para gráficas
    /// </summary>
    [HttpGet("{contadorId}/historico")]
    [ProducesResponseType(typeof(HistoricoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HistoricoResponse>> ObtenerHistorico(
        int contadorId,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] string granularidad = "hora") // hora, dia
    {
        try
        {
            var fechaHasta = hasta ?? DateTime.UtcNow;
            var fechaDesde = desde ?? fechaHasta.AddDays(-7);

            var contador = await _context.ContadoresDispositivo
                .Where(c => c.Id == contadorId)
                .Select(c => new { c.Id, c.Nombre })
                .FirstOrDefaultAsync();

            if (contador == null)
            {
                return NotFound(new { mensaje = "Contador no encontrado" });
            }

            var response = new HistoricoResponse
            {
                ContadorDispositivoId = contadorId,
                ContadorNombre = contador.Nombre,
                Desde = fechaDesde,
                Hasta = fechaHasta,
                Granularidad = granularidad,
                Datos = new List<DatoHistorico>()
            };

            if (granularidad == "hora")
            {
                var fechaDesdeOnly = DateOnly.FromDateTime(fechaDesde);
                var fechaHastaOnly = DateOnly.FromDateTime(fechaHasta);

                var resumenes = await _context.ResumenesProduccionHora
                    .Where(r => r.ContadorDispositivoId == contadorId
                             && r.Fecha >= fechaDesdeOnly
                             && r.Fecha <= fechaHastaOnly)
                    .OrderBy(r => r.Fecha)
                    .ThenBy(r => r.Hora)
                    .ToListAsync();

                response.Datos = resumenes.Select(r => new DatoHistorico
                {
                    Periodo = r.Fecha.ToDateTime(new TimeOnly(r.Hora, 0)),
                    Produccion = r.ProduccionTotal,
                    Resets = r.NumeroResets,
                    Lecturas = r.NumeroLecturas,
                    ContadorInicio = r.ContadorInicio,
                    ContadorFin = r.ContadorFin
                }).ToList();
            }
            else // dia
            {
                var fechaDesdeOnly = DateOnly.FromDateTime(fechaDesde);
                var fechaHastaOnly = DateOnly.FromDateTime(fechaHasta);

                var resumenes = await _context.ResumenesProduccionDia
                    .Where(r => r.ContadorDispositivoId == contadorId
                             && r.Fecha >= fechaDesdeOnly
                             && r.Fecha <= fechaHastaOnly)
                    .OrderBy(r => r.Fecha)
                    .ToListAsync();

                response.Datos = resumenes.Select(r => new DatoHistorico
                {
                    Periodo = r.Fecha.ToDateTime(TimeOnly.MinValue),
                    Produccion = r.ProduccionTotal,
                    Resets = r.NumeroResets,
                    Lecturas = r.NumeroLecturas
                }).ToList();
            }

            response.TotalProduccion = response.Datos.Sum(d => d.Produccion);
            response.TotalResets = response.Datos.Sum(d => d.Resets);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener histórico para contador {ContadorId}", contadorId);
            return StatusCode(500, new { mensaje = "Error interno" });
        }
    }

    /// <summary>
    /// Obtiene lecturas en tiempo real (últimos N minutos)
    /// </summary>
    [HttpGet("{contadorId}/lecturas")]
    [ProducesResponseType(typeof(LecturasRealtimeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LecturasRealtimeResponse>> ObtenerLecturasRealtime(
        int contadorId,
        [FromQuery] int ultimosMinutos = 120)
    {
        try
        {
            var desde = DateTime.UtcNow.AddMinutes(-ultimosMinutos);

            var contador = await _context.ContadoresDispositivo
                .Where(c => c.Id == contadorId)
                .Select(c => new { c.Id, c.Nombre })
                .FirstOrDefaultAsync();

            if (contador == null)
            {
                return NotFound(new { mensaje = "Contador no encontrado" });
            }

            var lecturas = await _context.LecturasContador
                .Where(l => l.ContadorDispositivoId == contadorId && l.FechaHoraLectura >= desde)
                .OrderBy(l => l.FechaHoraLectura)
                .Select(l => new PuntoLectura
                {
                    FechaHora = l.FechaHoraLectura,
                    Valor = l.ContadorValor,
                    ProduccionIncremental = l.ProduccionIncremental,
                    EsReset = l.EsReset
                })
                .ToListAsync();

            return Ok(new LecturasRealtimeResponse
            {
                ContadorDispositivoId = contadorId,
                ContadorNombre = contador.Nombre,
                Lecturas = lecturas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lecturas realtime para contador {ContadorId}", contadorId);
            return StatusCode(500, new { mensaje = "Error interno" });
        }
    }

    /// <summary>
    /// Obtiene todos los contadores dispositivo
    /// </summary>
    [HttpGet("dispositivos")]
    [ProducesResponseType(typeof(List<ContadorDispositivoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContadorDispositivoDto>>> ObtenerDispositivos([FromQuery] int? maquinaId = null)
    {
        try
        {
            var query = _context.ContadoresDispositivo.AsQueryable();

            if (maquinaId.HasValue)
                query = query.Where(c => c.MaquinaId == maquinaId.Value);

            var contadores = await query
                .Where(c => c.Activo)
                .OrderBy(c => c.MaquinaId)
                .ThenBy(c => c.Nombre)
                .Select(c => new ContadorDispositivoDto
                {
                    Id = c.Id,
                    MaquinaId = c.MaquinaId,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    TipoContador = c.TipoContador,
                    Activo = c.Activo,
                    FechaCreacion = c.FechaCreacion
                })
                .ToListAsync();

            return Ok(contadores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dispositivos");
            return StatusCode(500, new List<ContadorDispositivoDto>());
        }
    }

}
