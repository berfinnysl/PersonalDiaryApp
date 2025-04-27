using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PersonalDiaryApp.Data;
using PersonalDiaryApp.Entities;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ————————————————————————————————————————————
// 0) Connection String (appsettings.json içinde "DefaultConnection" olarak tanımlı olmalı)
// ————————————————————————————————————————————
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// ————————————————————————————————————————————
// 1) Controllers & Swagger (OpenAPI) + JWT Bearer tanımı
// ————————————————————————————————————————————
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PersonalDiaryApp API",
        Version = "v1",
        Description = "Kişisel Günlük Uygulaması API dokümantasyonu"
    });

    // Swagger UI'da JWT desteği ekleyelim
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT token'ınızı girin: **Bearer <token>**"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ————————————————————————————————————————————
// 2) DbContext & Identity
// ————————————————————————————————————————————
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connStr));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ————————————————————————————————————————————
// 3) JWT Authentication
// ————————————————————————————————————————————
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

        ValidIssuer = builder.Configuration["Jwt:Issuer"], // "PersonalDiaryApp"
        ValidAudience = builder.Configuration["Jwt:Audience"], // "PersonalDiaryUsers"
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]) // "BuCOKUZUNveGUVENLISecretKey_32ByteVeUzeri!"
        )
    };
});


builder.Services.AddAuthorization();

// ————————————————————————————————————————————
// 4) Middleware (Request Pipeline)
// ————————————————————————————————————————————
var app = builder.Build();

// 4.1) Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PersonalDiaryApp API V1");
    // Eğer Swagger UI’ı kök dizine almak isterseniz:
    // c.RoutePrefix = string.Empty;
});

// 4.2) HTTPS Redirection
app.UseHttpsRedirection();

// 4.3) Kimlik Doğrulama & Yetkilendirme
app.UseAuthentication();
app.UseAuthorization();

// 4.4) Statik Dosyalar (wwwroot altında)
app.UseStaticFiles();

// 4.5) Controller Routing
app.MapControllers();

// Uygulamayı başlat
app.Run();
