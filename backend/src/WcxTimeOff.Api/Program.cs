using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Configuration;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Data;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Middleware;
using WcxTimeOff.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection(SeedSettings.SectionName));

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    if (builder.Environment.IsDevelopment())
    {
        jwt.Key = "DEV-ONLY-wcx-timeoff-exam-jwt-key-change-me!";
        builder.Configuration["Jwt:Key"] = jwt.Key;
    }
    else
    {
        throw new InvalidOperationException("Jwt:Key must be set via configuration or the JWT__KEY environment variable.");
    }
}

builder.Services.PostConfigure<JwtSettings>(settings =>
{
    if (string.IsNullOrWhiteSpace(settings.Key))
    {
        settings.Key = jwt.Key;
    }
});

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICreditBalanceRepository, CreditBalanceRepository>();
builder.Services.AddScoped<ITimeOffRequestRepository, TimeOffRequestRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ITimeOffService, TimeOffService>();
builder.Services.AddScoped<SeedService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "The request is invalid." : e.ErrorMessage)
                .FirstOrDefault() ?? "The request is invalid.";
            return new BadRequestObjectResult(new ErrorResponse
            {
                Error = "invalid_request",
                Message = message
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            };
        };
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WCX Time-Off API",
        Version = "v1",
        Description = "Time-off credits, requests, and admin approvals."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var seedSettings = app.Configuration.GetSection(SeedSettings.SectionName).Get<SeedSettings>() ?? new SeedSettings();
if (seedSettings.Enabled)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<SeedService>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var mongo = app.Configuration["Mongo:ConnectionString"];
        logger.LogCritical(ex, "Startup seed failed. Confirm MongoDB is reachable at {MongoConnection}.", mongo);
        throw;
    }
}

app.Run();

public partial class Program;
