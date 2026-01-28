# Linealytics API

API REST para el sistema Linealytics que permite registrar paros de línea, contadores de producción y fallas desde dispositivos externos.

## Tecnologías

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core 8.0
- PostgreSQL (Npgsql)
- Swagger/OpenAPI
- DotNetEnv (gestión de variables de entorno)

## Configuración

### 1. Configurar Variables de Entorno

Este API usa variables de entorno en lugar de `appsettings.json` para mayor seguridad.

**Paso 1:** Copia el archivo de ejemplo:
```bash
cp .env.example .env
```

**Paso 2:** Edita `.env` y configura tus credenciales:
```bash
# Base de Datos PostgreSQL (misma que webapp_claude)
DB_HOST=localhost
DB_PORT=5432
DB_NAME=webapp_db
DB_USER=webapp_user
DB_PASSWORD=tu_password_real_aqui

# CORS (orígenes permitidos)
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173

# Swagger
ENABLE_SWAGGER=true
```

**Importante:**
- El archivo `.env` NO se sube a Git (está en `.gitignore`)
- El API comparte la misma base de datos que `webapp_claude`
- Las tablas usan el esquema `linealytics`
- Las migraciones las maneja `webapp_claude`, no este API

### 2. Ejecutar el API

```bash
cd LinealyticsAPI
dotnet run
```

El API estará disponible en:
- HTTP: `http://localhost:5160`
- HTTPS: `https://localhost:7228`
- Swagger UI: `http://localhost:5160/swagger` o `https://localhost:7228/swagger`

## Endpoints

### Paros de Línea

#### POST /api/ParosLinea/registrar
Registra un paro de línea. **Comportamiento automático:**
- Si NO existe un paro abierto para la máquina/departamento → **ABRE** un nuevo paro
- Si YA existe un paro abierto para la máquina/departamento → **CIERRA** el paro existente

Las fechas se calculan automáticamente por el sistema (UTC).

**Request Body:**
```json
{
  "maquinaId": 1,
  "departamentoId": 2,
  "operadorId": 5  // Opcional. Si es 0 o null, no se asigna operador
}
```

**Response (cuando ABRE un paro):**
```json
{
  "exitoso": true,
  "mensaje": "Paro de línea abierto exitosamente",
  "paroId": 15,
  "estado": "Abierto",
  "duracionMinutos": null
}
```

**Response (cuando CIERRA un paro):**
```json
{
  "exitoso": true,
  "mensaje": "Paro de línea cerrado exitosamente",
  "paroId": 15,
  "estado": "Cerrado",
  "duracionMinutos": 45
}
```

#### GET /api/ParosLinea/abiertos
Obtiene todos los paros de línea actualmente abiertos.

**Response:**
```json
[
  {
    "id": 15,
    "maquinaId": 1,
    "departamentoId": 2,
    "fechaHoraInicio": "2026-01-09T10:30:00Z",
    "fechaHoraFin": null,
    "duracionMinutos": null,
    "estado": "Abierto"
  }
]
```

### Fallas

#### POST /api/Fallas/insertar
Registra una falla específica en una máquina.

**Request Body:**
```json
{
  "fallaId": 5,  // ID de la causa de paro existente
  "maquinaId": 1,
  "modeloId": 3,  // Opcional
  "descripcion": "Atasco en alimentador de tornillos",  // Opcional
  "fechaHoraLectura": "2026-01-09T10:30:00Z"  // Opcional
}
```

**Response:**
```json
{
  "exitoso": true,
  "mensaje": "Falla registrada exitosamente",
  "fallaRegistroId": 28,
  "fechaHoraLectura": "2026-01-09T10:30:00Z"
}
```

#### GET /api/Fallas/ultimas?limite=10
Obtiene las últimas N fallas registradas.

**Query Parameters:**
- `limite` (opcional, default: 10): Número de registros a obtener

**Response:**
```json
[
  {
    "id": 28,
    "fallaId": 5,
    "maquinaId": 1,
    "fechaHoraLectura": "2026-01-09T10:30:00Z",
    "modeloId": 3,
    "descripcion": "Atasco en alimentador de tornillos"
  }
]
```

## Códigos de Estado HTTP

- `200 OK` - Operación exitosa
- `201 Created` - Recurso creado exitosamente
- `400 Bad Request` - Datos de entrada inválidos
- `404 Not Found` - Recurso no encontrado
- `500 Internal Server Error` - Error del servidor

## CORS

El API está configurado para aceptar solicitudes de cualquier origen. En producción, debes configurar orígenes específicos en `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins("https://tu-dominio.com")
                       .AllowAnyHeader()
                       .AllowAnyMethod());
});
```

## Notas Importantes

1. **Fechas y Horas**: Las fechas se calculan automáticamente por el sistema en UTC. No es necesario enviarlas.
2. **IDs de Referencia**: Los `maquinaId`, `departamentoId`, `fallaId`, y `modeloId` deben existir en la base de datos.
3. **Logging**: Los logs se guardan en la consola.
4. **Validación**: Todos los endpoints validan los datos de entrada. Revisa los mensajes de error en las respuestas.
5. **Paros de Línea**: Al llamar al endpoint `/registrar`, el sistema detecta automáticamente si debe abrir o cerrar el paro.

## Ejemplos con curl

### Registrar paro de línea (abre o cierra automáticamente)
```bash
curl -X POST "http://localhost:5160/api/ParosLinea/registrar" \
  -H "Content-Type: application/json" \
  -d '{"maquinaId": 1, "departamentoId": 2}'
```

### Insertar falla
```bash
curl -X POST "http://localhost:5000/api/Fallas/insertar" \
  -H "Content-Type: application/json" \
  -d '{"fallaId": 5, "maquinaId": 1, "descripcion": "Atasco en alimentador"}'
```

## Estructura del Proyecto

```
LinealyticsAPI/
├── Controllers/
│   ├── ParosLineaController.cs    # Endpoints para paros de línea
│   ├── ContadoresProduccionController.cs    # Endpoints para corridas y lecturas de producción
│   └── FallasController.cs        # Endpoints para fallas
├── Data/
│   └── LinealyticsDbContext.cs    # Contexto de Entity Framework
├── DTOs/
│   ├── ParoLineaDto.cs            # DTOs para paros
│   ├── ContadorDto.cs             # DTOs para contadores
│   └── FallaDto.cs                # DTOs para fallas
├── Models/
│   ├── RegistroParoBotonera.cs    # Modelo de paros
│   ├── RegistroContador.cs        # Modelo de contadores
│   └── RegistroFalla.cs           # Modelo de fallas
├── Program.cs                      # Configuración del API
├── appsettings.json               # Configuración
└── README.md                      # Esta documentación
```

## Desarrollo

Para agregar nuevos endpoints:

1. Crea el DTO correspondiente en la carpeta `DTOs/`
2. Agrega el controlador en `Controllers/`
3. Actualiza el `LinealyticsDbContext` si es necesario
4. La documentación de Swagger se generará automáticamente

## Soporte

Para reportar problemas o sugerencias, contacta al equipo de desarrollo.
