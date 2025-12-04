using ApiDePapas.Application.Interfaces;
using ApiDePapas.Application.Services;
using ApiDePapas.Infrastructure;
using ApiDePapas.Infrastructure.Persistence; // <-- 1. Añadimos el 'using' de la nueva clase
using ApiDePapas.Infrastructure.Repositories;
using ApiDePapas.Infrastructure.Auth;
using ApiDePapas.Infrastructure.Clients;
using ApiDePapas.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
// Ya no necesitamos 'MySqlConnector' aquí

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions => 
        {
            mySqlOptions.MigrationsAssembly("ApiDePapas.Infrastructure");
            mySqlOptions.EnableStringComparisonTranslations();
        }));

// Para habilitar Swagger / OpenAPI (documentación interactiva)
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        );
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- ADD CORS CONFIGURATION START ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000") // Allow your frontend origin
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});
// --- ADD CORS CONFIGURATION END ---

// --- JWT AUTHENTICATION CONFIGURATION START ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakConfig = builder.Configuration.GetSection("Authentication:Keycloak");
        var authority = keycloakConfig["Authority"];
        
        options.Authority = authority;
        options.RequireHttpsMetadata = false; // Solo para desarrollo local
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, // Keycloak no siempre incluye audience in client_credentials
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
            // NO usar RoleClaimType aquí, lo manejamos manualmente
        };
        
        // Logging para debugging (opcional)
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully");
                
                // Transformar los roles del token en claims de rol reconocidos por .NET
                var claimsIdentity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Buscar todos los claims "roles" (Keycloak puede enviarlos como múltiples claims)
                    var rolesClaims = claimsIdentity.FindAll("roles").ToList();
                    
                    foreach (var roleClaim in rolesClaims)
                    {
                        // Agregar cada rol como claim estándar de .NET
                        claimsIdentity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value));
                    }
                    
                    // Debug: Mostrar todos los claims
                    var claims = claimsIdentity.Claims.Select(c => $"{c.Type}: {c.Value}");
                    Console.WriteLine("All claims: " + string.Join(" | ", claims));
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
// --- JWT AUTHENTICATION CONFIGURATION END ---

//Registro de servicios

//builder.Services.AddScoped<IStockService, FakeStockService>();
builder.Services.AddHttpClient<IPurchasingService, PurchasingService>();
builder.Services.AddScoped<IShippingRepository, ShippingRepository>();
builder.Services.AddScoped<ICalculateCost, CalculateCost>();
builder.Services.AddScoped<TransportService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IDistanceService, DistanceServiceInternal>();
builder.Services.AddScoped<ILocalityRepository, LocalityRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ITravelRepository, TravelRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddSingleton<IShippingStore, ApiDePapas.Infrastructure.ShippingStore>();
builder.Services.AddHttpClient("KeycloakClient"); // HttpClient genérico para Keycloak
builder.Services.AddScoped<ITokenService, KeycloakTokenService>();
builder.Services.AddHttpClient<IStockService, StockApiClient>();

var app = builder.Build();

// Inicialización de base de datos
await DatabaseInitializer.InitializeDatabaseAsync(app.Services);

// Configurar pipeline HTTP

app.Use((context, next) =>
{
    // Pregunta: "¿Me mandó el Gateway algún prefijo?"
    var prefix = context.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault();
    
    // Si el Gateway nos dio un prefijo, ajustamos la base de la app
    if (!string.IsNullOrEmpty(prefix))
    {
        context.Request.PathBase = prefix;
    }
    return next();
});

// Habilitamos el uso de headers proxy estándar (X-Forwarded-For, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All
});

// --- FIN DEL BLOQUE ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFrontend");
app.UseAuthentication(); // DEBE ir antes de UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.Run();

// 3. Todo el código 'static async Task InitializeDatabaseAsync...'
//    y 'static async Task LoadCsvDataAsync...'
//    ha desaparecido de este archivo.