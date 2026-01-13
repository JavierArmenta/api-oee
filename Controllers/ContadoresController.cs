using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinealyticsAPI.Data;
using LinealyticsAPI.DTOs;
using LinealyticsAPI.Models;

namespace LinealyticsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContadoresController : ControllerBase
{
    private readonly LinealyticsDbContext _context;
    private readonly ILogger<ContadoresController> _logger;

    public ContadoresController(LinealyticsDbContext context, ILogger<ContadoresController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Registra contadores de producción (piezas OK y NOK).
    /// La fecha y hora se calculan automáticamente por el sistema.
    /// </summary>
    [HttpPost("insertar")]
    [ProducesResponseType(typeof(ContadorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContadorResponse>> InsertarContador([FromBody] InsertarContadorRequest request)
    {
        try
        {
            if (request.MaquinaId <= 0)
            {
                return BadRequest(new ContadorResponse
                {
                    Exitoso = false,
                    Mensaje = "MaquinaId debe ser mayor a 0"
                });
            }

            if (request.ContadorOK < 0 || request.ContadorNOK < 0)
            {
                return BadRequest(new ContadorResponse
                {
                    Exitoso = false,
                    Mensaje = "Los contadores no pueden ser negativos"
                });
            }

            var contador = new RegistroContador
            {
                MaquinaId = request.MaquinaId,
                ContadorOK = request.ContadorOK,
                ContadorNOK = request.ContadorNOK,
                ModeloId = request.ModeloId,
                FechaHoraLectura = DateTime.UtcNow  // Siempre calculado automáticamente
            };

            _context.RegistrosContador.Add(contador);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contador {ContadorId} registrado para máquina {MaquinaId}: OK={OK}, NOK={NOK}",
                contador.Id, contador.MaquinaId, contador.ContadorOK, contador.ContadorNOK);

            return CreatedAtAction(nameof(ObtenerUltimosContadores), new { limite = 1 }, new ContadorResponse
            {
                Exitoso = true,
                Mensaje = "Contador registrado exitosamente",
                ContadorId = contador.Id,
                TotalUnidades = contador.TotalUnidades,
                PorcentajeCalidad = Math.Round(contador.PorcentajeCalidad, 2),
                PorcentajeDefectos = Math.Round(contador.PorcentajeDefectos, 2)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar contador");
            return StatusCode(500, new ContadorResponse
            {
                Exitoso = false,
                Mensaje = "Error interno al registrar el contador"
            });
        }
    }

    /// <summary>
    /// Obtiene los últimos N contadores registrados
    /// </summary>
    /// <param name="limite">Número de registros a obtener (default: 10)</param>
    [HttpGet("ultimos")]
    [ProducesResponseType(typeof(List<ContadorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContadorDto>>> ObtenerUltimosContadores([FromQuery] int limite = 10)
    {
        try
        {
            if (limite <= 0)
            {
                limite = 10;
            }

            if (limite > 100)
            {
                limite = 100;
            }

            var contadores = await _context.RegistrosContador
                .OrderByDescending(c => c.FechaHoraLectura)
                .Take(limite)
                .Select(c => new ContadorDto
                {
                    Id = c.Id,
                    MaquinaId = c.MaquinaId,
                    ContadorOK = c.ContadorOK,
                    ContadorNOK = c.ContadorNOK,
                    FechaHoraLectura = c.FechaHoraLectura,
                    ModeloId = c.ModeloId
                })
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} contadores", contadores.Count);

            return Ok(contadores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener últimos contadores");
            return StatusCode(500, new List<ContadorDto>());
        }
    }
}
