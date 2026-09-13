# WARA API — Backend de Gestión de Trabajadores

Backend desarrollado en **C# .NET 8 (.NET Core)** siguiendo **Arquitectura Hexagonal**
(Ports & Adapters), como parte de la Evaluación Técnica solicitada por la empresa WARA.

## Arquitectura

```
src/
├── WARA.Domain/            → Núcleo del negocio. Entidades + Puertos (interfaces).
│                              No depende de NINGÚN otro proyecto ni framework externo.
├── WARA.Application/       → Casos de uso (Services). Implementa los puertos de entrada
│                              usando únicamente los puertos de salida del Domain.
├── WARA.Infrastructure/    → Adaptadores concretos: acceso a datos (Dapper + Stored
│                              Procedures), hashing de contraseñas (BCrypt), JWT.
└── WARA.API/                → Adaptador de entrada HTTP: Controllers, DTOs, Program.cs
                                (composition root), middleware de excepciones, Swagger.
```

**Regla de dependencia (clave de la arquitectura hexagonal):**
`API → Application → Domain ← Infrastructure`

El Domain nunca depende de Infrastructure ni de API. Esto permite, por ejemplo,
cambiar SQL Server por otro motor, o Dapper por Entity Framework, sin tocar una
sola línea de las reglas de negocio.

```
database/
└── script.sql              → Creación de BD, tablas, stored procedures y datos semilla
                                (trabajadores de ejemplo). No siembra usuarios: el sistema
                                no maneja roles, así que cualquiera se registra libremente
                                con /api/auth/register.
```

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, Developer o full) — o SQL Server en Docker
- Visual Studio Code con la extensión **C# Dev Kit** (recomendado)

## Pasos para ejecutar el proyecto

### 1. Restaurar dependencias

Desde la raíz del proyecto (`WARA.Backend/`):

```bash
dotnet restore
```

### 2. Crear la base de datos

Abre `database/script.sql` en SQL Server Management Studio (SSMS) o Azure Data
Studio y ejecútalo completo. Esto crea:
- La base de datos `WaraDB`
- Las tablas `Usuarios` y `Trabajadores`
- Los procedimientos almacenados usados por el backend
- Trabajadores de ejemplo

No siembra ningún usuario: el primero lo creas tú mismo llamando a
`POST /api/auth/register` una vez la API esté corriendo (ver sección
"Autenticación" más abajo).

### 3. Configurar la cadena de conexión

Edita `src/WARA.API/appsettings.Development.json` con tus credenciales reales
de SQL Server:

```json
"ConnectionStrings": {
  "WaraDatabase": "Server=localhost;Database=WaraDB;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
}
```

Si usas SQL Server Express con autenticación de Windows:

```json
"WaraDatabase": "Server=localhost\\SQLEXPRESS;Database=WaraDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

> **Nota de seguridad:** para un entorno real, la cadena de conexión y el
> `SecretKey` de JWT no deberían vivir en `appsettings.json` sino en
> `dotnet user-secrets` (el proyecto ya trae `UserSecretsId` configurado) o
> variables de entorno. Se dejan aquí en claro para simplificar la evaluación
> técnica.

### 4. Ejecutar la API

```bash
cd src/WARA.API
dotnet run
```

La API quedará disponible en algo como `https://localhost:7xxx` (el puerto exacto
se muestra en consola). Swagger UI estará disponible en la raíz `/swagger` en modo
Development.

## Autenticación

El sistema **no maneja roles ni jerarquía de usuarios** — el requerimiento solo
pide un login contra la tabla `Usuarios`, sin distinguir tipos de cuenta. Por
eso el modelo es simple:

- **Cualquiera puede crear su cuenta** con `POST /api/auth/register` (público,
  sin necesidad de estar logueado).
- **Cualquier cuenta registrada puede loguearse** con `POST /api/auth/login` y
  obtener un token JWT.
- **Ese token es lo único que protege** `/api/trabajadores` (GET y POST): no
  importa qué usuario seas, con un token válido tienes acceso completo. La
  autenticación aquí responde a "¿estás logueado?", no a "¿qué puedes hacer?".

## Endpoints disponibles

| Método | Ruta | Autenticación | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | No requiere | Autentica un usuario contra la tabla `Usuarios` |
| POST | `/api/auth/register` | No requiere | Registra una nueva cuenta de usuario |
| GET | `/api/auth/me` | **JWT requerido** | Devuelve Id y NombreUsuario del usuario autenticado (leído del JWT) |
| GET | `/api/trabajadores?dni=xxx&pagina=1&tamanoPagina=10` | **JWT requerido** | Lista trabajadores activos, paginado y filtrado por DNI |
| GET | `/api/trabajadores/{id}` | **JWT requerido** | Obtiene un trabajador puntual |
| POST | `/api/trabajadores` | **JWT requerido** | Registra un nuevo trabajador |
| PUT | `/api/trabajadores/{id}` | **JWT requerido** | Actualiza Nombre/Apellido/Edad (el DNI no es editable) |
| DELETE | `/api/trabajadores/{id}` | **JWT requerido** | Da de baja lógica a un trabajador (`Activo = 0`) |
| GET | `/health` | No requiere | Health check: verifica que la API y la conexión a SQL Server estén operativas |

Los endpoints marcados como "JWT requerido" esperan el header:

```
Authorization: Bearer {token obtenido en /api/auth/login}
```

Los cuatro endpoints de `/api/trabajadores` (GET por id, PUT, DELETE, y la
paginación en el GET raíz) exceden el alcance mínimo solicitado en el
requerimiento original (login + listar + agregar). Se agregaron porque el
reclutador confirmó que sumar funcionalidad adicional no tiene ningún
problema, y porque un CRUD de "gestión de trabajadores" sin poder editar o
dar de baja se sentía incompleto para un caso de uso real.

### Ejemplo — Registrar una cuenta

Endpoint público, no requiere token.

```http
POST /api/auth/register
Content-Type: application/json

{
  "nombreUsuario": "jorge.ruiz",
  "password": "MiPassword123!"
}
```

Respuesta (201 Created):

```json
{
  "esExitoso": true,
  "nombreUsuario": "jorge.ruiz",
  "mensaje": "Usuario registrado exitosamente."
}
```

Si el nombre de usuario ya existe, o el password tiene menos de 8 caracteres,
responde `400 Bad Request` con el mensaje correspondiente.

### Ejemplo — Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "nombreUsuario": "jorge.ruiz",
  "password": "MiPassword123!"
}
```

Respuesta (200 OK):

```json
{
  "esExitoso": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "nombreUsuario": "jorge.ruiz",
  "mensaje": "Inicio de sesión exitoso."
}
```

### Ejemplo — Listar trabajadores

```http
GET /api/trabajadores
GET /api/trabajadores?dni=712
```

### Ejemplo — Agregar trabajador

Requiere el header `Authorization: Bearer {token}` igual que el resto de
endpoints de `/api/trabajadores`.

```http
POST /api/trabajadores
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

{
  "nombre": "Ana",
  "apellido": "Torres Vega",
  "dni": "74567890",
  "edad": 25
}
```

Respuesta (201 Created):

```json
{
  "esExitoso": true,
  "mensaje": "Trabajador registrado exitosamente.",
  "trabajador": {
    "id": 4,
    "nombre": "Ana",
    "apellido": "Torres Vega",
    "dni": "74567890",
    "edad": 25
  }
}
```

### Ejemplo — Usuario actual (`/me`)

```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

Respuesta (200 OK):

```json
{
  "id": 1,
  "nombreUsuario": "jorge.ruiz"
}
```

### Ejemplo — Health check

No requiere autenticación; pensado para monitoreo externo (Docker, balanceadores, uptime).

```http
GET /health
```

Respuesta (200 OK si todo está saludable):

```json
{
  "estado": "Healthy",
  "duracionMs": 12.4,
  "checks": [
    {
      "nombre": "sql-server",
      "estado": "Healthy",
      "descripcion": "Conexión a SQL Server operativa.",
      "duracionMs": 11.8
    }
  ]
}
```

Si SQL Server no está disponible, responde `503 Service Unavailable` con
`"estado": "Unhealthy"` y la descripción del error.

## Decisiones técnicas

- **Dapper en lugar de Entity Framework**: se eligió Dapper porque el requerimiento
  pide explícitamente el uso de procedimientos almacenados para toda la lógica de
  acceso a datos. Dapper da control total del SQL ejecutado sin la sobrecarga de un
  ORM completo, siendo el enfoque más directo y transparente para este caso de uso.
- **JWT para autenticación, sin roles**: tras el login exitoso se emite un token
  JWT, siguiendo el estándar de la industria para autenticar llamadas posteriores
  desde la app móvil. El requerimiento no pide distinción de tipos de usuario, así
  que el token solo certifica "estoy logueado" — protege `/api/trabajadores`
  (GET/POST) por igual para cualquier cuenta. `/api/auth/login` y
  `/api/auth/register` quedan públicos a propósito: cualquiera puede crear su
  cuenta y usarla, sin que un usuario "admin" tenga que habilitarlo primero.
- **BCrypt para contraseñas**: nunca se almacenan contraseñas en texto plano.
- **Middleware de excepciones centralizado**: las reglas de negocio violadas (DNI
  duplicado, usuario duplicado, campos inválidos, etc.) lanzan excepciones de dominio
  que se traducen automáticamente a respuestas HTTP con código y mensaje adecuados
  (400/401), sin ensuciar los controllers con try/catch repetitivos.
- **CORS abierto**: habilitado para `AnyOrigin` únicamente para facilitar las pruebas
  desde el emulador/dispositivo Android durante la evaluación.

## Tests unitarios

El proyecto `tests/WARA.Tests` cubre `TrabajadorService` y `AuthService` con
**xUnit + Moq + FluentAssertions**, mockeando los repositorios (puertos de
salida) para que los tests corran en memoria, sin SQL Server levantado.

Para ejecutarlos, desde la raíz del proyecto (`WARA.Backend/`):

```bash
dotnet test
```

Casos cubiertos:
- Login exitoso, credenciales inválidas, usuario inexistente, usuario inactivo,
  y campos vacíos (sin llegar a consultar la BD).
- Registro exitoso (verifica que la contraseña se hashea antes de persistir),
  usuario duplicado, y password demasiado corto.
- Alta de trabajador válida, DNI duplicado, y cada validación de campo
  (nombre/apellido/DNI/edad) probada individualmente con `[Theory]`.
- Normalización de paginación (página/tamaño fuera de rango).
- Obtener por Id inexistente, actualizar con edad inválida, eliminar.

> **Nota:** `GET /api/auth/me` no tiene test unitario porque su lógica vive en
> el controller (lectura de claims de `ClaimsPrincipal`), no en un Service.
> Cubrirlo correctamente requeriría un test de integración con
> `WebApplicationFactory<Program>` simulando un `HttpContext` autenticado, lo
> cual excede el alcance de tests unitarios puros de este proyecto.

## Limitación conocida

La columna `Dni` en `Trabajadores` tiene un `UNIQUE` constraint físico a nivel
de tabla. La regla de negocio de unicidad (`sp_Trabajador_ExisteDni`) solo
considera trabajadores **activos**, pensando en permitir que el DNI de un
trabajador dado de baja pueda reutilizarse. Sin embargo, el constraint físico
igual lo rechazaría, porque el registro inactivo sigue ocupando ese valor de
DNI en la tabla. La solución correcta es reemplazar el `UNIQUE` de columna por
un **índice único filtrado** (`CREATE UNIQUE INDEX ... WHERE Activo = 1`), que
solo aplica la restricción de unicidad entre registros activos. No se
implementó para no ampliar más el alcance de la entrega, pero queda
documentado como mejora identificada.

## Próximos pasos (fuera del alcance de este backend)

- Prototipos de las interfaces del aplicativo móvil.
- Desarrollo del aplicativo Android (Kotlin) consumiendo estos endpoints vía Retrofit.
- Documentación de arquitectura y modelado de base de datos (diagrama ER).
