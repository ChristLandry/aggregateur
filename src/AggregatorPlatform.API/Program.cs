using System.Reflection;
using System.Text;
using AggregatorPlatform.API.HealthChecks;
using AggregatorPlatform.API.Logging;
using AggregatorPlatform.API.Middleware;
using AggregatorPlatform.API.Services;
using AggregatorPlatform.Application;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Infrastructure;
using AggregatorPlatform.Infrastructure.Persistence;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<PiiMaskingEnricher>());

// MVC
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// API services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentPartnerService, CurrentPartnerService>();

// Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opts.SaveToken = true;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(opts =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    opts.AddPolicy("Default", p =>
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// Health
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddCheck<ExternalApiHealthCheck>("external-apis");

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aggregator Platform API",
        Version = "v1",
        Description = "Backend agrégateur de flux financiers — orchestration banques / EME / wallets."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityDefinition("PartnerId", new OpenApiSecurityScheme
    {
        Description = "Partner identifier",
        Name = "X-Partner-Id",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }, Array.Empty<string>() },
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "PartnerId", Type = ReferenceType.SecurityScheme } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// Apply migrations + seed at startup (dev convenience)
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("AUTO_MIGRATE") == "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AggregatorDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Database migration failed at startup.");
    }

    // Auto-seed du partenaire WEB (technique, reserve a l'app frontoffice).
    try
    {
        await AggregatorPlatform.API.Services.WebPartnerSeeder
            .EnsureWebPartnerAsync(scope.ServiceProvider, log);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Web partner seeding failed at startup.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseMiddleware<GlobalExceptionHandler>();

app.UseHttpsRedirection();
app.UseCors("Default");

app.UseAuthentication();
app.UseMiddleware<PartnerAuthMiddleware>();
app.UseAuthorization();

app.UseHttpMetrics();
app.MapMetrics();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { }
