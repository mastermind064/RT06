using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Services;
using RTMultiTenant.Api.Validators;
using RTMultiTenant.Api.Extensions;
using FluentValidation.AspNetCore;
using FluentValidation;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                        "server=localhost;port=3306;database=rt_multi_tenant;user=root;password=root";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.34-mysql"),
        b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddScoped<MonthlySummaryUpdater>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddFluentValidationAutoValidation()
    .AddValidatorsFromAssemblyContaining<ResidentProfileRequestValidator>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret ?? JwtSettings.DefaultSecret))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole("ADMIN"));
    options.AddPolicy(AuthorizationPolicies.ResidentOnly, policy => policy.RequireRole("WARGA"));
});


// ---------- ADD THIS: CORS ----------
var corsPolicyName = "AllowAngularDev";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName, policy =>
    {
        policy.WithOrigins(
                    "http://localhost:4200", // Angular dev server
                    "https://localhost:55570/" // swagger 
               )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// ------------------------------------


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RT Multi Tenant API",
        Version = "v1",
        Description = "API backend untuk aplikasi RT dengan multi tenant dan JWT auth"
    });

    // 🧩 Tambahkan definisi security untuk Bearer JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Masukkan token JWT dengan format: **Bearer {token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // 🔐 Tambahkan requirement agar Swagger tahu semua endpoint bisa pakai Bearer
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

var uploadsPath = Path.Combine(app.Environment.WebRootPath!, "uploads"); // ⬅️ wwwroot/uploads
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads" // tetap /uploads, tapi sumbernya dari wwwroot/uploads
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ---------- USE CORS (place before auth) ----------
app.UseCors(corsPolicyName);
// --------------------------------------------------

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
