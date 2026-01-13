using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinealyticsAPI.Data;
using LinealyticsAPI.DTOs;
using LinealyticsAPI.Models;

namespace LinealyticsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FallasController : ControllerBase
{
    private readonly LinealyticsDbContext _context;
    private readonly ILogger<FallasController> _logger;

    public FallasController(LinealyticsDbContext context, ILogger<FallasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Registra una falla específica en una máquina.
    /// La fecha y hora se calculan automáticamente por el sistema.
    /// </summary>
    [HttpPost("insertar")]
    [ProducesResponseType(typeof(FallaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FallaResponse>> InsertarFalla([FromBody] InsertarFallaRequest request)
    {
        try
        {
            if (request.FallaId <= 0 || request.MaquinaId <= 0)
            {
                return BadRequest(new FallaResponse
                {
                    Exitoso = false,
                    Mensaje = "FallaId y MaquinaId deben ser mayores a 0"
                });
            }

            var falla = new RegistroFalla
            {
                FallaId = request.FallaId,
                MaquinaId = request.MaquinaId,
                ModeloId = request.ModeloId,
                Descripcion = request.Descripcion,
                FechaHoraLectura = DateTime.UtcNow  // Siempre calculado automáticamente
            };

            _context.RegistrosFalla.Add(falla);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Falla {FallaRegistroId} registrada para máquina {MaquinaId} - FallaId: {FallaId}",
                falla.Id, falla.MaquinaId, falla.FallaId);

            return CreatedAtAction(nameof(ObtenerUltimasFallas), new { limite = 1 }, new FallaResponse
            {
                Exitoso = true,
                Mensaje = "Falla registrada exitosamente",
                FallaRegistroId = falla.Id,
                FechaHoraLectura = falla.FechaHoraLectura
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar falla");
            return StatusCode(500, new FallaResponse
            {
                Exitoso = false,
                Mensaje = "Error interno al registrar la falla"
            });
        }
    }

    /// <summary>
    /// Obtiene las últimas N fallas registradas
    /// </summary>
    /// <param name="limite">Número de registros a obtener (default: 10)</param>
    [HttpGet("ultimas")]
    [ProducesResponseType(typeof(List<FallaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FallaDto>>> ObtenerUltimasFallas([FromQuery] int limite = 10)
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

            var fallas = await _context.RegistrosFalla
                .OrderByDescending(f => f.FechaHoraLectura)
                .Take(limite)
                .Select(f => new FallaDto
                {
                    Id = f.Id,
                    FallaId = f.FallaId,
                    MaquinaId = f.MaquinaId,
                    FechaHoraLectura = f.FechaHoraLectura,
                    ModeloId = f.ModeloId,
                    Descripcion = f.Descripcion
                })
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} fallas", fallas.Count);

            return Ok(fallas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener últimas fallas");
            return StatusCode(500, new List<FallaDto>());
        }
    }
}
