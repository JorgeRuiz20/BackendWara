using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WARA.API.HealthChecks;
using WARA.API.Middleware;
using WARA.Application.Services;
using WARA.Domain.Ports;
using WARA.Infrastructure.Persistence;
using WARA.Infrastructure.Repositories;
using WARA.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Soporte para Render y plataformas cloud que inyectan la variable de entorno PORT
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portEnv))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}

// Soporte para configuración local gitignored
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// ---------------------------------------------------------------------------
// 1. Configuración
// ---------------------------------------------------------------------------
var connectionString = Environment.GetEnvironmentVariable("WARA_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("WaraDatabase")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'WaraDatabase' en appsettings.json ni variables de entorno.");

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("No se encontró la sección 'Jwt' en appsettings.json.");

// ---------------------------------------------------------------------------
// 2. Inyección de dependencias — Infraestructura (adaptadores)
// ---------------------------------------------------------------------------
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Auto";
builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString, databaseProvider));
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITrabajadorRepository, TrabajadorRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

// ---------------------------------------------------------------------------
// 3. Inyección de dependencias — Aplicación (casos de uso)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITrabajadorService, TrabajadorService>();

// ---------------------------------------------------------------------------
// 4. Autenticación JWT
// ---------------------------------------------------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 5. CORS — para permitir el consumo desde el aplicativo móvil Android
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ---------------------------------------------------------------------------
// 6. Controllers + Swagger
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WARA API - Gestión de Trabajadores",
        Version = "v1",
        Description = "Web Services para la gestión de trabajadores de la empresa WARA. " +
                      "Arquitectura hexagonal (Ports & Adapters) con .NET Core."
    });

    var esquemaSeguridad = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT obtenido en /api/auth/login. Formato: Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", esquemaSeguridad);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { esquemaSeguridad, new[] { "Bearer" } }
    });
});

// ---------------------------------------------------------------------------
// 7. Health Checks — verifica que la API y su conexión a SQL Server estén operativas.
// Útil para monitoreo externo y como healthcheck de Docker/orquestadores.
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>("sql-server", tags: new[] { "ready" });

var app = builder.Build();

// ---------------------------------------------------------------------------
// 8. Pipeline HTTP
// ---------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger disponible para pruebas y documentación
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WARA API v1");
    options.RoutePrefix = "swagger";
});

// Redirigir la raíz al Swagger para facilidad de acceso en Render
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

app.UseCors("PermitirApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// GET /health — healthcheck profundo (incluye estado de conexión a la BD)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            estado = report.Status.ToString(),
            duracionMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                nombre = e.Key,
                estado = e.Value.Status.ToString(),
                descripcion = e.Value.Description,
                duracionMs = e.Value.Duration.TotalMilliseconds
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.Run();

// Necesario para que el proyecto sea testeable con WebApplicationFactory en pruebas de integración
public partial class Program { }
