using AnonymityAPI.Data;
using AnonymityAPI.Repositories;
using AnonymityAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
var jwtSettings = builder.Configuration.GetSection("Jwt");

if (!jwtSettings.Exists() ||
    string.IsNullOrWhiteSpace(jwtSettings["Key"]) ||
    string.IsNullOrWhiteSpace(jwtSettings["Issuer"]) ||
    string.IsNullOrWhiteSpace(jwtSettings["Audience"]))
{
    throw new Exception("JWT settings are missing or incomplete in configuration.");
}

var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var errorMessage = new
                    {
                        error = "TokenExpired",
                        message = "Your session has expired. Please log in again."
                    };
                    return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorMessage));
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Use CORS
app.UseCors("AllowLocalhost");

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Authentication & Authorization middleware (order matters)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
