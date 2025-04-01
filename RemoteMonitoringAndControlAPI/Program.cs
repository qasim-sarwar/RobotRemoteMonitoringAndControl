using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RemoteMonitoringAndControlAPI;

var builder = WebApplication.CreateBuilder(args);

// Dummy secret key for JWT (in production, use secure storage)
var jwtSecretKey = "ThisIsASecretKeyForJwtTokenGeneration!123";
var key = Encoding.ASCII.GetBytes(jwtSecretKey);

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true // Ensure token expiration is validated
    };
});

builder.Services.AddAuthorization();

// Add Swagger support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Robot Control API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' in the field below"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add In-Memory DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("InMemoryDb"));

builder.Services.AddScoped<ApplicationDbContext>();

var app = builder.Build();

// Inject logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () =>
{
    logger.LogInformation("Health check called");
    return Results.Ok("API is healthy");
});

// Login endpoint for JWT token generation
app.MapPost("/login", (UserCredentials credentials) =>
{
    logger.LogInformation("Login attempt by user: {Username}", credentials.Username);

    if (credentials.Username == "user" && credentials.Password == "password")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.Name, credentials.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        logger.LogInformation("JWT token generated for user: {Username}", credentials.Username);
        return Results.Ok(new { token = tokenHandler.WriteToken(token) });
    }

    logger.LogWarning("Unauthorized login attempt by user: {Username}", credentials.Username);
    return Results.Unauthorized();
});

// API Endpoints
app.MapPost("/command", async (Command command, ApplicationDbContext dbContext) =>
{
    logger.LogInformation("Received command to create: {CommandText}", command.CommandText);
    dbContext.Commands.Add(command);
    await dbContext.SaveChangesAsync();
    logger.LogInformation("Command created with ID: {CommandId}", command.Id);
    return Results.Ok(new { message = "Command accepted", commandId = command.Id });
}).RequireAuthorization();

app.MapPut("/command", async (int id, Command updatedCommand, ApplicationDbContext dbContext) =>
{
    logger.LogInformation("Received command update request for ID: {CommandId}", id);
    var command = await dbContext.Commands.FindAsync(id);
    if (command == null)
    {
        logger.LogWarning("Command not found: {CommandId}", id);
        return Results.NotFound("Command not found");
    }

    command.CommandText = updatedCommand.CommandText;
    command.Robot = updatedCommand.Robot;
    command.User = updatedCommand.User;
    await dbContext.SaveChangesAsync();

    logger.LogInformation("Command updated successfully: {CommandId}", id);
    return Results.Ok(new { message = "Command updated", updatedCommand });
}).RequireAuthorization();

app.MapGet("/command", async (int id, ApplicationDbContext dbContext) =>
{
    logger.LogInformation("Fetching command with ID: {CommandId}", id);
    var command = await dbContext.Commands.FindAsync(id);
    if (command == null)
    {
        logger.LogWarning("Command not found: {CommandId}", id);
        return Results.NotFound("Command not found");
    }
    return Results.Ok(command);
}).RequireAuthorization();

app.MapGet("/status", async (ApplicationDbContext dbContext) =>
{
    var robotStatus = await dbContext.RobotStatuses.FirstOrDefaultAsync() ?? new RobotStatus { Status = "Idle", Position = "0,0", Task = "None" };
    logger.LogInformation("Fetched robot status: {Status}", robotStatus.Status);
    return Results.Ok(robotStatus);
}).RequireAuthorization();

app.MapGet("/history", async (ApplicationDbContext dbContext) =>
{
    var commandHistory = await dbContext.Commands.ToListAsync();
    logger.LogInformation("Fetched command history: {CommandCount} commands", commandHistory.Count);
    return Results.Ok(commandHistory);
}).RequireAuthorization();

app.Run();

public record UserCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public partial class Program { }
