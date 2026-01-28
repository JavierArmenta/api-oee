using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinealyticsAPI.Data;
using LinealyticsAPI.DTOs;
using LinealyticsAPI.Models;

namespace LinealyticsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParosLineaController : ControllerBase
{
    private readonly LinealyticsDbContext _context;
    private readonly ILogger<ParosLineaController> _logger;

    public ParosLineaController(LinealyticsDbContext context, ILogger<ParosLineaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Registra un paro de línea. Si ya existe un paro abierto para la máquina/departamento, lo cierra.
    /// Si no existe, abre uno nuevo. Las fechas se calculan automáticamente.
    /// </summary>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(ParoLineaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParoLineaResponse>> RegistrarParoLinea([FromBody] RegistrarParoLineaRequest request)
    {
        try
        {
            // Validar que se proporcionen botonera y botón
            if (string.IsNullOrWhiteSpace(request.Botonera) || string.IsNullOrWhiteSpace(request.Boton))
            {
                return BadRequest(new ParoLineaResponse
                {
                    Exitoso = false,
                    Mensaje = "Botonera y Boton son requeridos"
                });
            }

            // Buscar la botonera por su número de serie
            var botonera = await _context.Botoneras
                .FirstOrDefaultAsync(b => b.NumeroSerie == request.Botonera);

            if (botonera == null)
            {
                return BadRequest(new ParoLineaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró la botonera con número de serie '{request.Botonera}'"
                });
            }

            // Buscar el botón por su código
            var boton = await _context.Botones
                .FirstOrDefaultAsync(b => b.Codigo == request.Boton);

            if (boton == null)
            {
                return BadRequest(new ParoLineaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró el botón con código '{request.Boton}'"
                });
            }

            // Obtener MaquinaId de la botonera y DepartamentoId del botón
            int maquinaId = botonera.MaquinaId;
            int departamentoId = boton.DepartamentoOperadorId;
            int botonId = boton.Id;

            // Validar OperadorId si se proporciona
            int? operadorId = null;
            if (request.OperadorId.HasValue && request.OperadorId.Value > 0)
            {
                var operadorExiste = await _context.Operadores
                    .AnyAsync(o => o.Id == request.OperadorId.Value);

                if (!operadorExiste)
                {
                    return BadRequest(new ParoLineaResponse
                    {
                        Exitoso = false,
                        Mensaje = $"No se encontró el operador con ID '{request.OperadorId.Value}'"
                    });
                }
                operadorId = request.OperadorId.Value;
            }

            // Buscar si existe un paro abierto para esta botonera (solo uno permitido por botonera)
            var paroAbierto = await _context.RegistrosParoBotonera
                .Where(p => p.BotoneraId == botonera.Id
                         && p.Estado == "Abierto")
                .OrderByDescending(p => p.FechaHoraInicio)
                .FirstOrDefaultAsync();

            if (paroAbierto != null)
            {
                // Verificar que el botón sea el mismo que abrió el paro
                if (paroAbierto.BotonId != botonId)
                {
                    _logger.LogWarning("Intento de cerrar paro {ParoId} con botón diferente. BotonId original: {BotonIdOriginal}, BotonId recibido: {BotonIdRecibido}",
                        paroAbierto.Id, paroAbierto.BotonId, botonId);

                    return BadRequest(new ParoLineaResponse
                    {
                        Exitoso = false,
                        Mensaje = $"No se puede cerrar el paro con un botón diferente. Use el mismo botón que lo abrió.",
                        ParoId = paroAbierto.Id,
                        Estado = "Abierto"
                    });
                }

                // CERRAR el paro existente (mismo botón)
                paroAbierto.FechaHoraFin = DateTime.UtcNow;
                paroAbierto.Estado = "Cerrado";
                paroAbierto.DuracionMinutos = (int)(paroAbierto.FechaHoraFin.Value - paroAbierto.FechaHoraInicio).TotalMinutes;

                // Actualizar OperadorId si se proporciona
                if (operadorId.HasValue)
                {
                    paroAbierto.OperadorId = operadorId;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Paro de línea {ParoId} CERRADO para botonera {Botonera}, botón {Boton}, máquina {MaquinaId} - Duración: {Duracion} minutos - Operador: {OperadorId}",
                    paroAbierto.Id, request.Botonera, request.Boton, paroAbierto.MaquinaId, paroAbierto.DuracionMinutos, paroAbierto.OperadorId);

                return Ok(new ParoLineaResponse
                {
                    Exitoso = true,
                    Mensaje = "Paro de línea cerrado exitosamente",
                    ParoId = paroAbierto.Id,
                    Estado = "Cerrado",
                    DuracionMinutos = paroAbierto.DuracionMinutos
                });
            }
            else
            {
                // ABRIR un nuevo paro
                var nuevoParo = new RegistroParoBotonera
                {
                    MaquinaId = maquinaId,
                    DepartamentoId = departamentoId,
                    OperadorId = operadorId,
                    BotonId = botonId,
                    BotoneraId = botonera.Id,
                    FechaHoraInicio = DateTime.UtcNow,
                    Estado = "Abierto"
                };

                _context.RegistrosParoBotonera.Add(nuevoParo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Paro de línea {ParoId} ABIERTO para botonera {Botonera}, botón {Boton}, máquina {MaquinaId} - Operador: {OperadorId}",
                    nuevoParo.Id, request.Botonera, request.Boton, nuevoParo.MaquinaId, nuevoParo.OperadorId);

                return Ok(new ParoLineaResponse
                {
                    Exitoso = true,
                    Mensaje = "Paro de línea abierto exitosamente",
                    ParoId = nuevoParo.Id,
                    Estado = "Abierto",
                    DuracionMinutos = null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar paro de línea para botonera {Botonera}, botón {Boton}", request.Botonera, request.Boton);
            return StatusCode(500, new ParoLineaResponse
            {
                Exitoso = false,
                Mensaje = "Error interno al registrar el paro de línea"
            });
        }
    }

    /// <summary>
    /// Registra un paro de línea desde PLC (parámetros por URL).
    /// Endpoint alternativo para PLCs que no pueden enviar JSON.
    /// </summary>
    [HttpGet("registrar-plc")]
    [ProducesResponseType(typeof(ParoLineaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParoLineaResponse>> RegistrarParoLineaPLC(
        [FromQuery] string botonera,
        [FromQuery] string boton,
        [FromQuery] int? operadorId = null)
    {
        var request = new RegistrarParoLineaRequest
        {
            Botonera = botonera,
            Boton = boton,
            OperadorId = operadorId
        };
        return await RegistrarParoLinea(request);
    }
}
