# Vigia del Rio Procesamiento

Módulo que se suscribe a un servidor MQTT para recibir datos, procesarlos y almacenarlos, permitiendo que la información esté disponible para su consumo a través de una API.

## Iniciar Proyecto

### Opción 1: Docker (Recomendado)

Esta opción levanta tanto la aplicación como una base de datos PostgreSQL configurada automáticamente.

1. Asegúrate de tener Docker y Docker Compose instalados.
2. Ejecuta el siguiente comando en la raíz del proyecto:

```bash
docker-compose up --build -d
```

La API estará disponible en `http://localhost:8080`.

### Opción 2: Ejecución Local (.NET)

Si prefieres ejecutarlo directamente con .NET:

```bash
dotnet run
```

> **Nota:** Para la ejecución local, asegúrate de tener una base de datos PostgreSQL corriendo en `localhost:5432` con las credenciales configuradas en `appsettings.json`.
