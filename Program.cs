using Microsoft.EntityFrameworkCore;
using LinealyticsAPI.Data;

// Cargar variables de entorno desde archivo .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configurar cadena de conexión desde variables de entorno
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=webapp;Username=webapp_user;Password=";

// Registrar DbContext con PostgreSQL
builder.Services.AddDbContext<LinealyticsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar CORS desde variables de entorno
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',')
    ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger solo si está habilitado
var enableSwagger = Environment.GetEnvironmentVariable("ENABLE_SWAGGER")?.ToLower() == "true"
    || builder.Environment.IsDevelopment();

if (enableSwagger)
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Linealytics API",
            Version = "v1",
            Description = "API REST para el sistema Linealytics - Registro de paros de línea, contadores y fallas"
        });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() && enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Linealytics API v1");
        c.RoutePrefix = "swagger";
    });
}


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5160); // HTTP
    // Para HTTPS:
    // options.ListenAnyIP(8443, o => o.UseHttps("rutaCert.pfx", "password"));
});


app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
