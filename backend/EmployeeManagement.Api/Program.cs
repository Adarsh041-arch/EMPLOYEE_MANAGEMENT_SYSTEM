using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.Services;
using EmployeeManagement.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var jwtKey = config["Jwt:Key"];
var encryptionKey = config["Encryption:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(encryptionKey))
    throw new InvalidOperationException(
        "Jwt:Key and Encryption:Key are missing. Copy appsettings.example.json to appsettings.Development.json and fill them in (see README).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(new SensitiveDataProtector(encryptionKey));
builder.Services.AddScoped<EmployeeCodeGenerator>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<TransferService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<EmployeeRequestValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy => policy
        .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Employee Management API", Version = "v1" });

    // adds the "Authorize" button so the JWT can be pasted in once
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Log in with POST /api/auth/login and paste the token here."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
        c.IncludeXmlComments(xmlFile);
});

var app = builder.Build();

// apply migrations and add sample data on startup (can be switched off with SeedOnStartup=false)
if (config.GetValue("SeedOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// needed so the integration tests can use WebApplicationFactory<Program>
public partial class Program { }
