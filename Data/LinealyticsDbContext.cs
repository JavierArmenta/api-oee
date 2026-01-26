using Microsoft.EntityFrameworkCore;
using LinealyticsAPI.Models;

namespace LinealyticsAPI.Data;

public class LinealyticsDbContext : DbContext
{
    public LinealyticsDbContext(DbContextOptions<LinealyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<RegistroParoBotonera> RegistrosParoBotonera { get; set; }
    public DbSet<RegistroContador> RegistrosContador { get; set; }
    public DbSet<RegistroFalla> RegistrosFalla { get; set; }
    public DbSet<Botonera> Botoneras { get; set; }
    public DbSet<Boton> Botones { get; set; }
    public DbSet<Operador> Operadores { get; set; }

    // Sistema de contadores con corridas
    public DbSet<CorridaProduccion> CorridasProduccion { get; set; }
    public DbSet<LecturaContador> LecturasContador { get; set; }

    // Tablas de referencia (solo lectura, gestionadas desde webapp-oee)
    public DbSet<Maquina> Maquinas { get; set; }
    public DbSet<Producto> Productos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de RegistroParoBotonera
        modelBuilder.Entity<RegistroParoBotonera>(entity =>
        {
            entity.ToTable("RegistrosParoBotonera", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaquinaId).IsRequired();
            entity.Property(e => e.DepartamentoId).IsRequired();
            entity.Property(e => e.OperadorId).IsRequired(false);
            entity.Property(e => e.FechaHoraInicio).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired();
        });

        // Configuración de RegistroContador
        modelBuilder.Entity<RegistroContador>(entity =>
        {
            entity.ToTable("RegistrosContadores", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaquinaId).IsRequired();
            entity.Property(e => e.ContadorOK).IsRequired();
            entity.Property(e => e.ContadorNOK).IsRequired();
            entity.Property(e => e.FechaHoraLectura).IsRequired();
            entity.Ignore(e => e.TotalUnidades);
            entity.Ignore(e => e.PorcentajeCalidad);
            entity.Ignore(e => e.PorcentajeDefectos);
        });

        // Configuración de RegistroFalla
        modelBuilder.Entity<RegistroFalla>(entity =>
        {
            entity.ToTable("RegistrosFallas", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FallaId).IsRequired();
            entity.Property(e => e.MaquinaId).IsRequired();
            entity.Property(e => e.FechaHoraLectura).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
        });

        // Configuración de Botonera
        modelBuilder.Entity<Botonera>(entity =>
        {
            entity.ToTable("Botoneras", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NumeroSerie).HasMaxLength(50);
            entity.Property(e => e.DireccionIP).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        });

        // Configuración de Boton
        modelBuilder.Entity<Boton>(entity =>
        {
            entity.ToTable("Botones", "planta");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        });

        // Configuración de Operador
        modelBuilder.Entity<Operador>(entity =>
        {
            entity.ToTable("Operadores", "operadores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Apellido).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NumeroEmpleado).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CodigoPinHashed).HasMaxLength(255);
        });

        // Configuración de CorridaProduccion
        modelBuilder.Entity<CorridaProduccion>(entity =>
        {
            entity.ToTable("CorridasProduccion", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaquinaId).IsRequired();
            entity.Property(e => e.ProductoId).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => new { e.MaquinaId, e.Estado });
            entity.HasIndex(e => new { e.MaquinaId, e.FechaInicio });
        });

        // Configuración de LecturaContador
        modelBuilder.Entity<LecturaContador>(entity =>
        {
            entity.ToTable("LecturasContador", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorridaId).IsRequired();
            entity.Property(e => e.MaquinaId).IsRequired();
            entity.Property(e => e.ProductoId).IsRequired();
            entity.HasIndex(e => new { e.MaquinaId, e.FechaHoraLectura });
            entity.HasIndex(e => new { e.CorridaId, e.FechaHoraLectura });
        });

        // Configuración de Maquina (referencia solo lectura - schema planta)
        modelBuilder.Entity<Maquina>(entity =>
        {
            entity.ToTable("Maquinas", "planta");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        });

        // Configuración de Producto (referencia solo lectura - schema linealytics)
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos", "linealytics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Codigo).HasMaxLength(50);
        });
    }
}
