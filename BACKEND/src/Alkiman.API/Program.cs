using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Alkiman.API.Authorization;
using Alkiman.API.Configuration;
using Alkiman.API.Middleware;
using Alkiman.API.Authentication;
using Alkiman.API.RateLimiting;
using Alkiman.API.Services;
using Alkiman.Application;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Common.Permissions;
using Alkiman.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Descifrado de configuración ───────────────────────────────────────────────
// Los valores ENC(...) en appsettings se descifran en memoria usando la clave
// CONFIG_ENCRYPTION_KEY del entorno. Si no está definida (p. ej. en desarrollo)
// los valores quedan tal cual. Debe correr antes de leer cualquier secreto.
builder.Configuration.DecryptEncryptedValues();

// Quitar la cabecera "Server: Kestrel" de todas las respuestas
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

// ── Servicios de aplicación ────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentLandlordService, CurrentLandlordService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ── Autenticación: JWT propio (emitido por AuthController) ────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Falta configurar 'Jwt:Secret' (User Secrets o variable de entorno).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // El 'sub' del token es el UserId (el LandlordId viaja en 'landlord_id');
        // no remapeamos los claims a esquemas legacy de .NET para poder leerlos
        // tal cual en CurrentUserService/CurrentLandlordService.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        // Firma y vencimiento no alcanzan: un usuario eliminado o desactivado seguiría
        // entrando hasta que caduque su token. Acá se confirma contra la base.
        options.Events = ActiveUserJwtEvents.Create();
    });

// Falla el arranque si un endpoint exige un permiso de módulo pero se olvidaron de
// ponerle [RequireModule]: quedaría abierto a negocios que no compraron el módulo.
ModuleGuardValidator.Validate(typeof(Program).Assembly);

// ── Autorización: una policy por permiso del catálogo (claim 'permission') ────
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCatalog.All)
    {
        options.AddPolicy(permission.Code, policy => policy.RequireClaim("permission", permission.Code));
    }
});

// ── CORS (Frontend React) ──────────────────────────────────────────────────────
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var frontendOrigins = builder.Configuration.GetSection("Cors:FrontendOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Rate limiting por IP ───────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Red de seguridad global: 300 req/min por IP para todos los endpoints.
    // Los endpoints específicos añaden políticas más estrictas encima de este límite.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Auth: ventana deslizante para mitigar brute-force en login/registro
    // 10 req/min por IP (además del límite global de 300)
    options.AddPolicy(RateLimitPolicies.Auth, context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(context),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Portales y kioscos públicos: moderado para carga legítima sin excesos
    // 60 req/min por IP
    options.AddPolicy(RateLimitPolicies.Public, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    static string GetClientIp(HttpContext ctx)
    {
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
});

// ── Controllers + Swagger (con soporte de Bearer JWT) ─────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums como texto ("Available") en vez de números (0) en el JSON:
        // más legible/mantenible para el frontend y para debugging.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Alkiman API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT propio (emitido por /api/auth/login o /api/auth/register). Formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ════════════════════════════════════════════════════════════════════════════════

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Solo en Development: es donde se agrega un módulo y donde tiene que saltar que el
    // catálogo de permisos del código y el seed de la base quedaron desincronizados.
    await PermissionSeedValidator.ValidateAsync(app.Services);
}

// ── Pipeline de middleware (orden importa) ────────────────────────────────────
// 1. Captura todas las excepciones (debe ser primero)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Cabeceras de seguridad en todas las respuestas (incluyendo errores)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 3. Bloqueo de IPs en lista negra y detección de abuso de volumen
app.UseMiddleware<IpBlocklistMiddleware>();

// 4. HTTPS redirect fuera de desarrollo
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// 5. CORS antes del rate limiter (preflight OPTIONS no debe consumir cuota)
app.UseCors(FrontendCorsPolicy);

// 6. Rate limiting por IP (se evalúa después de identificar el origen CORS)
app.UseRateLimiter();

// 7. Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
