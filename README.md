# WARA API — Backend de Gestión de Trabajadores

Backend empresarial desarrollado en **C# .NET 8 LTS** siguiendo **Arquitectura Hexagonal (Puertos y Adaptadores / Clean Architecture)**, con persistencia de alto rendimiento mediante **Dapper Micro-ORM**, ejecución de **Procedimientos Almacenados**, seguridad criptográfica con **BCrypt** y **JWT Bearer**, y despliegue continuo en la nube sobre **Render Cloud** con base de datos administrada **MySQL 8.0 en Aiven Cloud**.

Este proyecto forma parte de la Evaluación Técnica integral para la empresa **WARA**.

---

## 🌐 Servicios en Producción Cloud

* **Base URL de la API:** [https://backendwara.onrender.com/](https://backendwara.onrender.com/)
* **Health Check Público:** [https://backendwara.onrender.com/api/health](https://backendwara.onrender.com/api/health)
* **Documentación Interactiva Swagger:** [https://backendwara.onrender.com/swagger](https://backendwara.onrender.com/swagger)
* **Base de Datos Cloud:** MySQL 8.0 alojada en **Aiven Cloud** con cifrado SSL obligatorio (`SslMode=Required`).
* **Aplicativo Móvil Complementario:** [AppWara en GitHub](https://github.com/JorgeRuiz20/AppWara.git) (Android nativo en Kotlin con Jetpack Compose y Clean MVI).
* **Prototipos Interactivos de Diseño:** [Figma Workspace & Prototypes](https://www.figma.com/design/q8VWiKmXt16dmHJkWwEqxN/wara-prototipo?node-id=0-1&t=jFH60rnI88HSWprI-1).

---

## 🏛️ Arquitectura del Sistema

La solución adopta el patrón de **Arquitectura Hexagonal / Onion Architecture**:

```
WARA.Backend/
├── src/
│   ├── WARA.Domain/            → Núcleo del negocio. Entidades puras (Usuario, Trabajador) 
│   │                              y Puertos/Interfaces. Totalmente desacoplado de frameworks.
│   ├── WARA.Application/       → Casos de uso y orquestación (AuthService, TrabajadorService),
│   │                              DTOs con validaciones estrictas y reglas de negocio.
│   ├── WARA.Infrastructure/    → Adaptadores de salida: Persistencia con Dapper, mapeo tipado
│   │                              a Stored Procedures MySQL, PasswordHasher (BCrypt) y TokenGenerator (JWT).
│   └── WARA.API/               → Adaptador de entrada HTTP: Controllers (Auth, Trabajadores, Health),
│                                  Middlewares (manejo global de excepciones RFC 7807), filtros y Swagger.
├── database/
│   ├── script.sql              → Script original de creación.
│   └── script_mysql.sql        → Script completo DDL/DML para MySQL 8.0 con 9 Stored Procedures.
└── tests/
    └── WARA.Tests/             → Suite de pruebas unitarias con xUnit, Moq y FluentAssertions (27 tests).
```

### Regla de Dependencia Estricta
$$\text{WARA.API} \longrightarrow \text{WARA.Application} \longrightarrow \text{WARA.Domain} \longleftarrow \text{WARA.Infrastructure}$$

* El **Domain** nunca referencia a la infraestructura ni a frameworks externos.
* La persistencia utiliza **Dapper Micro-ORM** en lugar de Entity Framework Core debido a que la totalidad de las operaciones están encapsuladas en **Procedimientos Almacenados**, logrando tiempos de respuesta de milisegundos y un consumo mínimo de memoria RAM (~120 MB en contenedor Docker).

---

## 🔐 Seguridad y Autenticación

1. **Tokens JWT (JSON Web Tokens):**
   * Autenticación stateless firmada con algoritmo criptográfico **HMAC-SHA256**.
   * Encapsula los claims estandarizados del usuario (`Sub`, `UniqueName`, `Jti`).
   * Tiempo de vida configurado a 120 minutos.
2. **Cifrado de Contraseñas con BCrypt:**
   * Las contraseñas nunca se almacenan en texto claro. Se procesan con salt dinámico y factor de trabajo calibrado contra ataques de fuerza bruta.
3. **Política Estricta de Contraseñas:**
   * Validación cruzada en DTOs (`RegularExpression`) y en capa de dominio (`AuthService`):
     * Longitud mínima: **8 caracteres**.
     * Al menos **una letra mayúscula** (`A-Z`).
     * Al menos **un símbolo o carácter especial** (`@`, `!`, `#`, `$`, `%`, etc.).
   * Se aplica tanto en el **Registro** como en el **Inicio de Sesión**.
4. **Manejo Centralizado de Excepciones:**
   * Las reglas de negocio infringidas lanzan `BusinessRuleException`, transformadas de inmediato en respuestas HTTP `400 Bad Request` o `401 Unauthorized` estructuradas, protegiendo las trazas internas del servidor.

---

## 📋 Catálogo de Endpoints RESTful

| Verbo | Ruta | Autenticación | Código HTTP | Descripción |
|---|---|---|---|---|
| **GET** | `/api/health` | **Público** | `200 OK` | Liveness check ligero y comprobación de operatividad (HealthController). |
| **GET** | `/health` | **Público** | `200 / 503` | Health check profundo de ASP.NET Core con test de conectividad activa a MySQL. |
| **POST** | `/api/auth/register` | **Público** | `201 Created` | Registro de usuario con validación estricta de contraseña y unicidad. |
| **POST** | `/api/auth/login` | **Público** | `200 OK` | Autenticación de credenciales y generación del token JWT Bearer. |
| **GET** | `/api/auth/me` | **JWT Requerido** | `200 OK` | Retorna Id y Nombre de usuario leyendo los claims del JWT actual. |
| **GET** | `/api/trabajadores` | **JWT Requerido** | `200 OK` | Listado de trabajadores activos, con soporte a filtro por DNI y paginación. |
| **GET** | `/api/trabajadores/{id}` | **JWT Requerido** | `200 / 404` | Obtiene el detalle de un trabajador específico por su ID. |
| **POST** | `/api/trabajadores` | **JWT Requerido** | `201 Created` | Registra un nuevo colaborador en la organización. |
| **PUT** | `/api/trabajadores/{id}` | **JWT Requerido** | `200 OK` | Actualiza nombre, apellido y edad de un trabajador existente. |
| **DELETE**| `/api/trabajadores/{id}` | **JWT Requerido** | `200 / 404` | Ejecuta la baja lógica (*soft delete*, `estado = 0`), preservando histórico. |

> **Nota:** Todos los endpoints protegidos requieren la cabecera:
> ```http
> Authorization: Bearer <token_jwt_obtenido_en_login>
> ```

---

## 💡 Ejemplos de Uso (Payloads JSON)

### 1. Health Check (`GET /api/health`)
```http
GET /api/health
```
**Respuesta (200 OK):**
```json
{
  "estado": "OK",
  "mensaje": "WARA API operativa",
  "timestamp": "2026-09-13T22:00:00.000Z"
}
```

### 2. Registro de Usuario (`POST /api/auth/register`)
La contraseña debe tener mínimo 8 caracteres, al menos una mayúscula y un símbolo.
```http
POST /api/auth/register
Content-Type: application/json

{
  "nombreUsuario": "jorge.ruiz",
  "password": "Password123*"
}
```
**Respuesta (201 Created):**
```json
{
  "esExitoso": true,
  "nombreUsuario": "jorge.ruiz",
  "mensaje": "Usuario registrado exitosamente."
}
```

### 3. Inicio de Sesión (`POST /api/auth/login`)
```http
POST /api/auth/login
Content-Type: application/json

{
  "nombreUsuario": "jorge.ruiz",
  "password": "Password123*"
}
```
**Respuesta (200 OK):**
```json
{
  "esExitoso": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "nombreUsuario": "jorge.ruiz",
  "mensaje": "Inicio de sesión exitoso."
}
```

### 4. Alta de Trabajador (`POST /api/trabajadores`)
```http
POST /api/trabajadores
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "nombre": "Carlos",
  "apellido": "Mendoza Díaz",
  "dni": "74892015",
  "edad": 32
}
```
**Respuesta (201 Created):**
```json
{
  "esExitoso": true,
  "mensaje": "Trabajador registrado exitosamente.",
  "trabajador": {
    "id": 15,
    "nombre": "Carlos",
    "apellido": "Mendoza Díaz",
    "dni": "74892015",
    "edad": 32
  }
}
```

---

## 🚀 Guía de Ejecución Local

### Prerrequisitos
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Servidor MySQL 8.0 (Local, Docker o Aiven Cloud).

### 1. Clonar el repositorio
```bash
git clone https://github.com/JorgeRuiz20/BackendWara.git
cd BackendWara
```

### 2. Configurar la Base de Datos
Si utilizas una instancia MySQL local o remota propia, ejecuta el script ubicado en:
```
database/script_mysql.sql
```

### 3. Configurar Cadena de Conexión
Edita el archivo `src/WARA.API/appsettings.json` o establece la variable de entorno correspondiente:
```json
"ConnectionStrings": {
  "WaraDatabase": "Server=mysql-1b091ec1-jorger180206-dbd8.c.aivencloud.com;Port=27274;Database=defaultdb;User Id=avnadmin;Password=TU_PASSWORD;SslMode=Required;"
}
```

### 4. Restaurar y Ejecutar
```bash
dotnet restore
dotnet run --project src/WARA.API
```
La API estará disponible en `http://localhost:5000` (o el puerto asignado en consola) y Swagger UI en `/swagger`.

---

## 🐳 Despliegue con Docker

El proyecto incluye un `Dockerfile` optimizado en **construcción multi-etapa (multi-stage build)**:
* **Fase de Build:** `mcr.microsoft.com/dotnet/sdk:8.0` para compilar y optimizar dependencias en modo Release.
* **Fase de Runtime:** `mcr.microsoft.com/dotnet/aspnet:8.0` generando una imagen final ultraligera de ~120 MB.

### Comandos Docker:
```bash
# Construir imagen
docker build -t wara-backend-api .

# Ejecutar contenedor vinculando variables de entorno
docker run -d -p 8080:8080 \
  -e ConnectionStrings__WaraDatabase="Server=...;Database=defaultdb;User Id=...;Password=...;SslMode=Required;" \
  -e Jwt__SecretKey="TU_CLAVE_SECRETA_JWT_MINIMO_32_CARACTERES" \
  --name wara-api wara-backend-api
```

---

## 🧪 Pruebas Unitarias Automatizadas

La suite de pruebas ubicada en `tests/WARA.Tests` garantiza la estabilidad de la lógica de negocio mediante **xUnit**, **Moq** y **FluentAssertions**:
* **Cobertura completa de reglas de negocio:**
  * Login con credenciales válidas, credenciales incorrectas, usuarios inexistentes o inactivos.
  * Validación estricta de contraseñas (rechazo de claves sin mayúsculas, sin símbolos o menores a 8 caracteres).
  * Registro de usuarios con detección de nombres duplicados y hashing obligatorio antes de persistir.
  * Validaciones de trabajadores (validación de DNI a 8 dígitos, rango de edad de 18 a 80 años, nombres obligatorios).
  * Normalización de paginación y verificación de bajas lógicas.
* **Aislamiento total:** Los repositorios se mockean en memoria, permitiendo que la suite se ejecute en segundos sin depender de servidores externos.

### Ejecución de Pruebas:
```bash
dotnet test
```
**Resultado:**
```
Serie de pruebas para WARA.Tests.dll (.NETCoreApp,Version=v8.0)
Pruebas totales: 27 | Correcto: 27 | Con error: 0 | Omitido: 0 | Tiempo: ~3.1 s
```

---

## 👤 Autor

* **Jorge Ruiz Tapia** — Desarrollador Senior Full Stack (.NET Core / Android Kotlin)
* **Repositorio del Backend:** [https://github.com/JorgeRuiz20/BackendWara.git](https://github.com/JorgeRuiz20/BackendWara.git)
* **Repositorio del Cliente Móvil:** [https://github.com/JorgeRuiz20/AppWara.git](https://github.com/JorgeRuiz20/AppWara.git)
