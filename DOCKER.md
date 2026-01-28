# Guía de Docker para Linealytics API

## Archivos Docker Creados

- **Dockerfile**: Configuración multi-stage para compilar y ejecutar la API
- **docker-compose.yml**: Orquestación de la API con PostgreSQL
- **.dockerignore**: Optimización del contexto de build

## Requisitos Previos

- Docker instalado (v20.10+)
- Docker Compose instalado (v2.0+)

## Uso Básico

### Opción 1: Docker Compose (Recomendado)

Ejecutar la API con PostgreSQL incluido:

```bash
# Iniciar todos los servicios
docker-compose up -d

# Ver logs
docker-compose logs -f api

# Detener servicios
docker-compose down

# Detener y eliminar volúmenes (datos)
docker-compose down -v
```

### Opción 2: Dockerfile Individual

Si solo quieres construir la imagen de la API:

```bash
# Construir la imagen
docker build -t linealytics-api:latest .

# Ejecutar el contenedor
docker run -p 5000:8080 \
  -e CONNECTION_STRING="Host=tu-host;Port=5432;Database=webapp;Username=webapp_user;Password=tu-password" \
  -e ALLOWED_ORIGINS="http://localhost:3000" \
  linealytics-api:latest
```

## Variables de Entorno

Crea un archivo `.env` en la raíz del proyecto con las siguientes variables:

```bash
# Base de datos
DB_PASSWORD=tu-password-seguro

# CORS
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173

# Swagger
ENABLE_SWAGGER=true

# Entorno
ASPNETCORE_ENVIRONMENT=Development
```

## Puertos Expuestos

- **API**: `5000` → `8080` (interno)
- **PostgreSQL**: `5432` → `5432`

## Acceso a la API

Una vez iniciado, la API estará disponible en:

- API Base: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger (si ENABLE_SWAGGER=true)

## Comandos Útiles

```bash
# Reconstruir las imágenes
docker-compose build --no-cache

# Ver contenedores en ejecución
docker-compose ps

# Acceder a la base de datos
docker-compose exec postgres psql -U webapp_user -d webapp

# Ver logs de un servicio específico
docker-compose logs -f postgres
docker-compose logs -f api

# Ejecutar migraciones (si es necesario)
docker-compose exec api dotnet ef database update
```

## Solución de Problemas

### La API no se conecta a la base de datos

Verifica que el contenedor de PostgreSQL esté saludable:
```bash
docker-compose ps
```

### Cambios en el código no se reflejan

Reconstruye la imagen:
```bash
docker-compose up -d --build
```

### Ver logs detallados

```bash
docker-compose logs -f
```

## Producción

Para producción, considera:

1. Cambiar `ASPNETCORE_ENVIRONMENT` a `Production`
2. Deshabilitar Swagger (`ENABLE_SWAGGER=false`)
3. Usar contraseñas seguras
4. Configurar CORS con orígenes específicos
5. Usar volúmenes externos para PostgreSQL
6. Implementar HTTPS con un reverse proxy (nginx, traefik)
