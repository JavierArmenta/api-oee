# Linealytics API - Arquitectura

API REST para monitoreo de producción manufacturera (OEE). Captura datos en tiempo real de líneas de producción a través de botoneras físicas: paros de línea, contadores de producción y fallas de máquinas.

---

## Stack Tecnológico

| Componente | Tecnología |
|------------|------------|
| Framework | .NET 8.0 / ASP.NET Core Web API |
| ORM | Entity Framework Core 8.0 |
| Base de datos | PostgreSQL (Npgsql) |
| Documentación | Swagger / OpenAPI |
| Config | DotNetEnv (.env files) |

---

## Estructura de Directorios

```
api-oee/
├── Controllers/           # Endpoints de la API
│   ├── ParosLineaController.cs    # Gestión de paros
│   ├── ContadoresProduccionController.cs    # Contadores y corridas de producción
│   └── FallasController.cs        # Registro de fallas
├── Models/                # Entidades de base de datos
│   ├── RegistroParoBotonera.cs    # Modelo de paro
│   ├── RegistroContador.cs        # Modelo de contador
│   ├── RegistroFalla.cs           # Modelo de falla
│   ├── Botonera.cs                # Caja de botones
│   ├── Boton.cs                   # Botón individual
│   └── Operador.cs                # Operador/empleado
├── DTOs/                  # Objetos de transferencia
│   ├── ParoLineaDto.cs
│   ├── ContadorDto.cs
│   └── FallaDto.cs
├── Data/
│   └── LinealyticsDbContext.cs    # DbContext EF Core
├── Program.cs             # Entrada y configuración
├── .env                   # Variables de entorno
└── LinealyticsAPI.csproj  # Dependencias NuGet
```

---

## Endpoints API

### Paros de Línea `/api/ParosLinea`

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/registrar` | Abre o cierra paro (toggle automático) |
| GET | `/abiertos` | Lista paros activos |

**Request POST /registrar:**
```json
{
  "botonera": "BTNR-1",
  "boton": "BTN-1",
  "operadorId": 5
}
```

**Lógica Toggle:** Si no hay paro abierto para la **botonera** → abre nuevo. Si existe paro abierto **y el botón es el mismo** → lo cierra y calcula duración. **Solo se permite 1 paro abierto por botonera. Solo se puede cerrar con el mismo botón que lo abrió.**

---

### Fallas `/api/Fallas`

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/insertar` | Registra falla de máquina |
| GET | `/ultimas?limite=10` | Últimas N fallas |

**Request POST /insertar:**
```json
{
  "fallaId": 5,
  "maquinaId": 1,
  "modeloId": 3,
  "descripcion": "Atasco en alimentador"
}
```

---

## Modelos de Datos

### RegistroParoBotonera
```
linealytics.RegistrosParoBotonera
├── Id (PK)
├── MaquinaId (FK)
├── DepartamentoId (FK)
├── OperadorId (FK, nullable)
├── BotonId (FK, nullable)
├── BotoneraId (FK) ← Limita a 1 paro abierto por botonera
├── FechaHoraInicio
├── FechaHoraFin (nullable)
├── DuracionMinutos (calculado al cerrar)
└── Estado ("Abierto" | "Cerrado")
```

### RegistroContador
```
linealytics.RegistrosContadores
├── Id (PK)
├── MaquinaId (FK)
├── ContadorOK
├── ContadorNOK
├── ModeloId (FK, nullable)
└── FechaHoraLectura
```

### RegistroFalla
```
linealytics.RegistrosFallas
├── Id (PK)
├── FallaId (FK - tipo de falla)
├── MaquinaId (FK)
├── ModeloId (FK, nullable)
├── Descripcion (max 500)
└── FechaHoraLectura
```

### Entidades de Soporte
```
Botonera (linealytics.Botoneras)
├── Id, Nombre, DireccionIP, NumeroSerie, MaquinaId

Boton (planta.Botones)
├── Id, Nombre, Codigo, DepartamentoOperadorId

Operador (operadores.Operadores)
├── Id, Nombre, Apellido, NumeroEmpleado, CodigoPinHashed
```

---

## Configuración

### Variables de Entorno (.env)

```env
ASPNETCORE_ENVIRONMENT=Development
CONNECTION_STRING=Host=localhost;Port=5432;Database=webapp;Username=admin;Password=admin123
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173
ENABLE_SWAGGER=true
```

| Variable | Descripción |
|----------|-------------|
| CONNECTION_STRING | Conexión PostgreSQL |
| ALLOWED_ORIGINS | URLs permitidas para CORS (separadas por coma) |
| ENABLE_SWAGGER | Habilitar documentación Swagger |

---

## Inicio Rápido

```bash
# 1. Configurar variables
cp .env.example .env
# Editar .env con credenciales de BD

# 2. Restaurar dependencias
dotnet restore

# 3. Ejecutar
dotnet run

# URLs disponibles:
# - API: http://localhost:5160
# - Swagger: http://localhost:5160/swagger
```

---

## Archivos Clave

| Archivo | Propósito |
|---------|-----------|
| `Program.cs` | Configuración de servicios, CORS, Swagger |
| `Controllers/ParosLineaController.cs` | Lógica toggle de paros |
| `Controllers/ContadoresProduccionController.cs` | Gestión de corridas y lecturas de producción |
| `Data/LinealyticsDbContext.cs` | Mapeo de esquemas BD |
| `Models/*.cs` | Entidades y validaciones |

---

## Seguridad

- **Autenticación**: No implementada (API pública)
- **CORS**: Configurado desde `ALLOWED_ORIGINS`
- **Validación**: En controladores (existencia de entidades, valores positivos)
- **HTTPS**: Redireccionamiento habilitado en producción
