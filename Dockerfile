# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto y restaurar dependencias
COPY LinealyticsAPI.csproj .
RUN dotnet restore "LinealyticsAPI.csproj"

# Copiar el resto de los archivos y compilar
COPY . .
RUN dotnet build "LinealyticsAPI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "LinealyticsAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Exponer puerto (por defecto .NET 8 usa 8080)
EXPOSE 8080

# Copiar los archivos publicados
COPY --from=publish /app/publish .

# Copiar archivo de ejemplo de variables de entorno (opcional)
COPY .env.example .env.example

# Configurar usuario no-root para seguridad
USER $APP_UID

# Punto de entrada
ENTRYPOINT ["dotnet", "LinealyticsAPI.dll"]
