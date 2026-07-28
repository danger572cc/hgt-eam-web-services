# hgt-eam-web-services
EAM WebServices para HGT (anteriormente SAAM Terminals)

## 📋 Descripción

Este proyecto es una API RESTful desarrollada en .NET 9.0 que actúa como **gateway** para los servicios web de **INFOR EAM (Enterprise Asset Management)**. Su propósito principal es proporcionar acceso simplificado y optimizado a las grillas de datos de EAM, implementando un sistema de caché inteligente para garantizar respuestas rápidas y confiables.

### Características Principales

- ✅ **Integración con INFOR EAM**: Consume servicios SOAP de INFOR EAM para obtener datos de grillas
- ✅ **Sistema de caché con SQLite**: Almacenamiento local persistente para optimizar consultas
- ✅ **Paginación eficiente**: Manejo de grandes volúmenes de datos con paginación
- ✅ **Documentación automática con Scalar**: Endpoints autodocumentados usando OpenAPI
- ✅ **Autenticación Basic Auth**: Endpoints protegidos con autenticación básica
- ✅ **Arquitectura limpia**: Separación de responsabilidades (Controllers, Queries, Handlers, Infrastructure)
- ✅ **Rate Limiting**: Limitación de tasa de 60 requests/minuto por usuario o IP

---

## 🏗️ Arquitectura

El proyecto sigue los principios de **Clean Architecture** y utiliza los siguientes patrones:

### Patrones de Diseño
- **CQRS (Command Query Responsibility Segregation)**: Separación de comandos y consultas
- **Mediator Pattern**: Desacoplamiento de controllers y handlers mediante `IMediator`
- **Repository Pattern**: Abstracción del acceso a datos con caché
- **Dependency Injection**: Inyección de dependencias nativa de .NET

### Estructura de Capas

```
┌─────────────────────────────────────────────────────┐
│           HGT.EAM.WebServices (API)                 │
│  ├── Application/                                   │
│  │   ├── Controllers/    (Endpoints REST)           │
│  │   ├── Queries/        (Queries CQRS)             │
│  │   └── Models/         (DTOs)                     │
│  └── Setup/              (Configuración)            │
├─────────────────────────────────────────────────────┤
│     HGT.EAM.WebServices.Infrastructure              │
│  └── Architecture/                                  │
│      ├── Controller/     (Controladores base)       │
│      ├── Query/          (Interfaces CQRS)          │
│      ├── GridCache/      (Sistema de caché)         │
│      └── Models/         (Modelos compartidos)      │
├─────────────────────────────────────────────────────┤
│       HGT.EAM.WebServices.Conector                  │
│  └── Architecture/                                  │
│      ├── Interfaces/     (IEamGridFetcher, etc.)    │
│      └── Models/         (Modelos de EAM)           │
└─────────────────────────────────────────────────────┘
                      ↓
            INFOR EAM Web Services (SOAP)
```

---

## 🚀 Inicio Rápido

> **Nota:** esta sección cubre únicamente la **ejecución en desarrollo**.
> Para instalar el servicio en un servidor (IIS), ver la [Guía de instalación](#-documentación).

### Requisitos

- .NET 9.0 SDK o superior
- Credenciales de acceso a INFOR EAM Web Services
- SQL Server accesible **para el registro de eventos** (sink de Serilog). Los datos de negocio
  **no** provienen de SQL Server: se obtienen de los servicios SOAP de INFOR EAM.

### Ejecución en desarrollo

1. **Clonar el repositorio**:
   ```bash
   git clone <repository-url>
   cd hgt-eam-web-services
   ```

2. **Restaurar dependencias**:
   ```bash
   dotnet restore
   ```

3. **Ejecutar el proyecto**:
   ```bash
   dotnet run --project HGT.EAM.WebServices/HGT.EAM.WebServices.csproj
   ```

   Por defecto `dotnet run` toma la configuración de `launchSettings.json`. Para forzar la URL o el
   ambiente por variables de entorno, agregar `--no-launch-profile`.

---

## 📡 Documentación de API

El proyecto utiliza **Scalar** para documentación interactiva de la API basada en OpenAPI.

### Acceder a la Documentación

Una vez que la aplicación esté en ejecución, acceda a la documentación interactiva donde podrá:

- ✅ Ver todos los endpoints disponibles
- ✅ Consultar parámetros requeridos y opcionales
- ✅ Ver ejemplos de solicitudes y respuestas
- ✅ Probar endpoints directamente desde el navegador

La interfaz Scalar proporciona toda la información necesaria sobre:
- Categorías de endpoints (Abastecimiento, Contabilidad, Cuentas por Pagar, Control de Gestión)
- Parámetros de consulta
- Códigos de respuesta HTTP
- Modelos de datos

---

## 🗄️ Sistema de Caché

El proyecto implementa un sistema de caché inteligente con **SQLite** para optimizar el rendimiento:

### Funcionamiento

1. **Consulta inicial**: Verifica si los datos están en caché
2. **Cache Miss**: Si no están, consulta INFOR EAM Web Services
3. **Almacenamiento**: Guarda los resultados en SQLite
4. **Consultas posteriores**: Lee directamente del caché

### Ventajas

- ⚡ **Respuestas ultra-rápidas** para datos previamente consultados
- 📊 **Reducción de carga** en servidores INFOR EAM
- 🔒 **Persistencia** de datos entre reinicios
- 📄 **Paginación eficiente** sin re-consultar el servicio

---

## 🔐 Autenticación y Seguridad

### Autenticación

Todos los endpoints requieren **autenticación básica (Basic Auth)**.

**Formato del header:**
```
Authorization: Basic <base64(username:password)>
```

### Rate Limiting

El proyecto implementa limitación de tasa para proteger el servicio:

- **Límite**: 60 requests por minuto
- **Por**: Usuario autenticado o dirección IP (si es anónimo)
- **Respuesta cuando se excede**: HTTP 429 (Too Many Requests)

---

## 🛠️ Tecnologías Utilizadas

| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| .NET | 9.0 | Framework principal |
| ASP.NET Core | 9.0 | Web API |
| Mediator | Latest | CQRS pattern |
| Mapster | Latest | Object mapping |
| SQLite | Latest | Sistema de caché |
| Serilog | Latest | Logging estructurado |
| Scalar | 2.11.0 | Documentación OpenAPI |

---

## 🐛 Troubleshooting

### Error: "The entity type 'GridCacheFieldEntity' cannot be configured"

**Solución**: Verificar que la base de datos de caché se haya creado correctamente. El proyecto crea automáticamente la base de datos SQLite al iniciar.

### Caché no se actualiza

**Solución**: Eliminar el archivo de caché para forzar una nueva consulta a INFOR EAM. El archivo
es `gridcache.db` y está en la raíz de la aplicación (según `ConnectionStrings:GridCache`), junto
con sus archivos auxiliares `-wal` y `-shm`:

```bash
rm gridcache.db gridcache.db-wal gridcache.db-shm
```

Eliminarlo no produce pérdida de información: su contenido es una copia de datos que residen en
INFOR EAM. La aplicación lo recrea al arrancar. El estado del archivo puede consultarse en el panel
de diagnóstico, en `/diagnostics`.

### HTTP 429 - Too Many Requests

**Solución**: Se ha excedido el límite de 60 requests por minuto. Esperar antes de realizar más solicitudes.

---

## 📚 Documentación

Además de la documentación interactiva de endpoints (Scalar), existen dos guías formales en la
biblioteca de documentación, en
`D:\Aplicaciones\CLAUDE\02_projects\HGT\hgt-eam-web-services\docs\`:

| Documento | Contenido |
|---|---|
| **Guía de instalación** | Requisitos, publicación por perfil, instalación en IIS, configuración, verificación y solución de problemas. |
| **Guía técnica** | Arquitectura, recorrido de una petición, mecanismo de carga por lotes desde EAM, caché, seguridad y observabilidad. |
| **Anexo — Valores por ambiente** | Datos concretos de cada instalación. |

Cada una está disponible en Markdown (fuente de verdad) y en `.docx` para entrega.

---

## 🩺 Panel de diagnóstico

La aplicación expone un panel interno en `/diagnostics`, protegido con las mismas credenciales de
la API. Permite verificar el estado de la conexión con EAM, validar credenciales por organización,
revisar la salud del archivo de caché, consultar métricas de rendimiento y visualizar el registro
de eventos tanto del archivo local como de la base de datos.

---

## 📄 Licencia

Este proyecto es propiedad de **HGT (SAAM Terminals)** y está destinado únicamente para uso interno.

---

**Última actualización**: 2026-07-28
