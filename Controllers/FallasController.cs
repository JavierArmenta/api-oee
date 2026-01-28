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
    /// Registra una falla específica en una máquina usando IDs.
    /// La fecha y hora se calculan automáticamente por el sistema.
    /// </summary>
    [HttpPost("insertar")]
    [ProducesResponseType(typeof(FallaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FallaResponse>> InsertarFalla([FromBody] InsertarFallaRequest request)
    {
        try
        {
            if (request.CatalogoFallaId <= 0 || request.MaquinaId <= 0)
            {
                return BadRequest(new FallaResponse
                {
                    Exitoso = false,
                    Mensaje = "CatalogoFallaId y MaquinaId deben ser mayores a 0"
                });
            }

            var fechaDeteccion = DateTime.UtcNow;
            var falla = new RegistroFalla
            {
                CatalogoFallaId = request.CatalogoFallaId,
                MaquinaId = request.MaquinaId,
                FechaHoraDeteccion = fechaDeteccion,
                Estado = "Pendiente",
                FechaCreacion = fechaDeteccion
            };

            _context.RegistrosFalla.Add(falla);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Falla {FallaRegistroId} registrada para máquina {MaquinaId} - CatalogoFallaId: {CatalogoFallaId}",
                falla.Id, falla.MaquinaId, falla.CatalogoFallaId);

            return CreatedAtAction(nameof(ObtenerUltimasFallas), new { limite = 1 }, new FallaResponse
            {
                Exitoso = true,
                Mensaje = "Falla registrada exitosamente",
                FallaRegistroId = falla.Id,
                FechaHoraDeteccion = falla.FechaHoraDeteccion
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
    /// Registra una falla usando códigos de máquina y falla en lugar de IDs.
    /// La fecha y hora se calculan automáticamente por el sistema.
    /// </summary>
    [HttpPost("insertar-por-codigo")]
    [ProducesResponseType(typeof(FallaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FallaResponse>> InsertarFallaPorCodigo([FromBody] InsertarFallaPorCodigoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CodigoMaquina))
            {
                return BadRequest(new FallaResponse
                {
                    Exitoso = false,
                    Mensaje = "CodigoMaquina es requerido"
                });
            }

            if (string.IsNullOrWhiteSpace(request.CodigoFalla))
            {
                return BadRequest(new FallaResponse
                {
                    Exitoso = false,
                    Mensaje = "CodigoFalla es requerido"
                });
            }

            // Buscar máquina por código
            var maquina = await _context.Maquinas
                .FirstOrDefaultAsync(m => m.Codigo == request.CodigoMaquina && m.Activo);

            if (maquina == null)
            {
                return NotFound(new FallaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró máquina activa con código '{request.CodigoMaquina}'"
                });
            }

            // Buscar catálogo de falla por código
            var catalogoFalla = await _context.CatalogoFallas
                .FirstOrDefaultAsync(c => c.Codigo == request.CodigoFalla && c.Activo);

            if (catalogoFalla == null)
            {
                return NotFound(new FallaResponse
                {
                    Exitoso = false,
                    Mensaje = $"No se encontró falla activa con código '{request.CodigoFalla}'"
                });
            }

            var fechaDeteccion = DateTime.UtcNow;
            var falla = new RegistroFalla
            {
                CatalogoFallaId = catalogoFalla.Id,
                MaquinaId = maquina.Id,
                FechaHoraDeteccion = fechaDeteccion,
                Estado = "Pendiente",
                FechaCreacion = fechaDeteccion
            };

            _context.RegistrosFalla.Add(falla);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Falla registrada por código - Id: {FallaRegistroId}, Máquina: {CodigoMaquina} (Id:{MaquinaId}), Falla: {CodigoFalla} (Id:{CatalogoFallaId})",
                falla.Id, request.CodigoMaquina, maquina.Id, request.CodigoFalla, catalogoFalla.Id);

            return CreatedAtAction(nameof(ObtenerUltimasFallas), new { limite = 1 }, new FallaResponse
            {
                Exitoso = true,
                Mensaje = $"Falla '{catalogoFalla.Nombre}' registrada en máquina '{maquina.Nombre}'",
                FallaRegistroId = falla.Id,
                FechaHoraDeteccion = falla.FechaHoraDeteccion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar falla por código");
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
                .OrderByDescending(f => f.FechaHoraDeteccion)
                .Take(limite)
                .Select(f => new FallaDto
                {
                    Id = f.Id,
                    CatalogoFallaId = f.CatalogoFallaId,
                    MaquinaId = f.MaquinaId,
                    FechaHoraDeteccion = f.FechaHoraDeteccion,
                    Estado = f.Estado
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
