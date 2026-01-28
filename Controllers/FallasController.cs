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

            return Created("", new FallaResponse
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
    /// Registra una falla usando códigos desde PLC (parámetros por URL).
    /// Endpoint alternativo para PLCs que no pueden enviar JSON.
    /// </summary>
    [HttpGet("insertar-por-codigo-plc")]
    [ProducesResponseType(typeof(FallaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FallaResponse>> InsertarFallaPorCodigoPLC(
        [FromQuery] string codigoMaquina,
        [FromQuery] string codigoFalla)
    {
        var request = new InsertarFallaPorCodigoRequest
        {
            CodigoMaquina = codigoMaquina,
            CodigoFalla = codigoFalla
        };
        return await InsertarFallaPorCodigo(request);
    }
}
