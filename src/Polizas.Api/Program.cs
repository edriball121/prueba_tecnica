using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;
using Polizas.Application.DTOs;
using Polizas.Application.Options;
using Polizas.Application.UseCases.Clientes;
using Polizas.Application.UseCases.Pagos;
using Polizas.Application.UseCases.Polizas;
using Polizas.Application.UseCases.Reportes;
using Polizas.Domain.Exceptions;
using Polizas.Infrastructure.Extensions;

// Npgsql: asegurar que todos los DateTime se traten como UTC sin conversión de zona horaria
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuración de servicios
// ---------------------------------------------------------------------------

// Options
builder.Services.Configure<TimezoneOptions>(
    builder.Configuration.GetSection(TimezoneOptions.SectionName));

// Infrastructure (DbContext + repositorios)
builder.Services.AddInfrastructure(builder.Configuration);

// Use cases (capa Application)
builder.Services.AddScoped<CreateClienteUseCase>();
builder.Services.AddScoped<CreatePolizaUseCase>();
builder.Services.AddScoped<RegistrarPagoUseCase>();
builder.Services.AddScoped<GetPolizaEstadoUseCase>();
builder.Services.AddScoped<GetCarteraVencidaUseCase>();

// OpenAPI — metadata del spec
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title       = "Polizas API",
            Version     = "v1",
            Description = "API REST para gestión de pólizas de seguro con cobros recurrentes. " +
                          "Soporta idempotencia en pagos mediante el header **Idempotency-Key**.",
            Contact = new() { Name = "Transfiriendo", Email = "dev@transfiriendo.com" }
        };
        return Task.CompletedTask;
    });
});

// ProblemDetails para respuestas de error estándar RFC 7807
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------------------
var app = builder.Build();
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Middleware de manejo de errores (debe ir antes de MapOpenApi y los endpoints)
// ---------------------------------------------------------------------------
app.UseExceptionHandler(exApp => exApp.Run(async ctx =>
{
    var feature = ctx.Features.Get<IExceptionHandlerFeature>();
    var ex = feature?.Error;

    ctx.Response.ContentType = "application/problem+json";

    var (statusCode, title) = ex switch
    {
        PagoDuplicadoException       => (StatusCodes.Status409Conflict,            "Pago duplicado"),
        EntidadNoEncontradaException => (StatusCodes.Status404NotFound,            "Recurso no encontrado"),
        DomainValidationException    => (StatusCodes.Status400BadRequest,          "Datos inválidos"),
        DomainException              => (StatusCodes.Status400BadRequest,          "Error de negocio"),
        _                            => (StatusCodes.Status500InternalServerError, "Error interno")
    };

    ctx.Response.StatusCode = statusCode;

    await ctx.Response.WriteAsJsonAsync(new
    {
        type    = $"https://httpstatuses.com/{statusCode}",
        title,
        status  = statusCode,
        detail  = ex?.Message
    });
}));

// ---------------------------------------------------------------------------
// Pipeline HTTP
// ---------------------------------------------------------------------------
// OpenAPI JSON spec: /openapi/v1.json
app.MapOpenApi();

// Scalar UI: /scalar/v1
app.MapScalarApiReference(options =>
{
    options.Title             = "Polizas API";
    options.Theme             = ScalarTheme.Purple;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// HTTPS redirect solo fuera de contenedor Docker (en producción se termina en el proxy)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ---------------------------------------------------------------------------
// Endpoint raíz (informativo)
// ---------------------------------------------------------------------------
app.MapGet("/", () => Results.Json(new { nombre = "WebApi Polizas", version = "1.0.0" }))
.WithName("Root")
.WithTags("Info")
.WithSummary("Info de la API");

// ---------------------------------------------------------------------------
// POST /clientes — Crear un nuevo cliente
// ---------------------------------------------------------------------------
app.MapPost("/clientes", async (
    CreateClienteRequest request,
    CreateClienteUseCase uc,
    CancellationToken ct) =>
{
    var response = await uc.EjecutarAsync(request, ct);
    return Results.Created($"/clientes/{response.Id}", response);
})
.WithName("CrearCliente")
.WithTags("Clientes")
.WithSummary("Crear un cliente")
.WithDescription("Registra un nuevo cliente. El campo `documento` debe ser único en el sistema.");

// ---------------------------------------------------------------------------
// POST /polizas — Crear una nueva póliza
// ---------------------------------------------------------------------------
app.MapPost("/polizas", async (
    CreatePolizaRequest request,
    CreatePolizaUseCase uc,
    CancellationToken ct) =>
{
    var response = await uc.EjecutarAsync(request, ct);
    return Results.Created($"/polizas/{response.Id}", response);
})
.WithName("CrearPoliza")
.WithTags("Pólizas")
.WithSummary("Crear una póliza")
.WithDescription("Crea una póliza asociada a un cliente existente. " +
                 "Si un beneficiario (por `documento`) ya existe en el sistema se reutiliza; " +
                 "si no, se crea automáticamente.");

// ---------------------------------------------------------------------------
// POST /polizas/{id}/pagos — Registrar un pago sobre una póliza (idempotente)
// ---------------------------------------------------------------------------
app.MapPost("/polizas/{id:long}/pagos", async (
    long id,
    RegistrarPagoRequest request,
    HttpContext httpContext,
    RegistrarPagoUseCase uc,
    CancellationToken ct) =>
{
    // El header Idempotency-Key es obligatorio para garantizar idempotencia
    if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues)
        || string.IsNullOrWhiteSpace(keyValues))
    {
        return Results.Problem(
            detail: "El header Idempotency-Key es requerido.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    var response = await uc.EjecutarAsync(id, request, keyValues.ToString(), ct);
    return Results.Ok(response);
})
.WithName("RegistrarPago")
.WithTags("Pagos")
.WithSummary("Registrar un pago")
.WithDescription("Registra un pago total o parcial sobre una póliza. " +
                 "**El header `Idempotency-Key` es obligatorio** (UUID único por intento). " +
                 "Reenviar la misma key retorna 409 sin duplicar el pago.");

// ---------------------------------------------------------------------------
// GET /polizas/{id}/estado — Consultar el estado de cartera de una póliza
// ---------------------------------------------------------------------------
app.MapGet("/polizas/{id:long}/estado", async (
    long id,
    GetPolizaEstadoUseCase uc,
    CancellationToken ct) =>
{
    var response = await uc.EjecutarAsync(id, ct);
    return Results.Ok(response);
})
.WithName("GetPolizaEstado")
.WithTags("Pólizas")
.WithSummary("Consultar estado de cartera")
.WithDescription("Retorna el estado (`al_dia` / `en_mora`), saldo pendiente, " +
                 "fecha del último pago y días de mora. " +
                 "El estado se calcula usando la zona horaria **America/Bogotá**.");

// ---------------------------------------------------------------------------
// GET /reportes/cartera-vencida — Reporte de pólizas en mora > 30 días
// ---------------------------------------------------------------------------
app.MapGet("/reportes/cartera-vencida", async (
    GetCarteraVencidaUseCase uc,
    CancellationToken ct) =>
{
    var items = await uc.EjecutarAsync(ct);
    return Results.Ok(items);
})
.WithName("GetCarteraVencida")
.WithTags("Reportes")
.WithSummary("Cartera vencida")
.WithDescription("Lista las pólizas con saldo pendiente y **más de 30 días** desde su fecha de vencimiento, " +
                 "calculado en zona horaria America/Bogotá.");

app.Run();
