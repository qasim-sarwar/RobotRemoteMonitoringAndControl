using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RemoteMonitoringAndControlAPI;
using Microsoft.AspNetCore.Mvc;

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
            new string[] { }
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

// Health check endpoint updated to return plain text
app.MapGet("/health", () =>
{
    logger.LogInformation("Health check called");
    // Return plain text content to avoid JSON serialization issues (no WriteAsJsonAsync involved)
    return Results.Content("API is healthy", "text/plain");
});

// Login endpoint for JWT token generation
app.MapPost("/login", async (UserCredentials credentials) =>
{
    logger.LogInformation("Login attempt by user: {Username}", credentials?.Username);

    if (credentials is null)
    {
        return Results.Problem("Credentials are null");
    }

    // Simulate async behavior (e.g., database call)
    await Task.Yield();

    if (credentials.Username == "user" && credentials.Password == "password")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, credentials.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        // Manually serialize to avoid WriteAsJsonAsync issues.
        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new { token = jwt });
        return Results.Content(jsonResponse, "application/json");
    }

    return Results.Unauthorized();
});

// POST /command endpoint: creates a new command.
app.MapPost("/command", async (Command command, ApplicationDbContext dbContext) =>
{
    try
    {
        dbContext.Commands.Add(command);
        await dbContext.SaveChangesAsync();
        var payload = new { message = "Command accepted", commandId = command.Id };
        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(payload);
        return Results.Content(jsonResponse, "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ ERROR in /command endpoint");
        return Results.Problem($"Error: {ex.Message}");
    }
}).RequireAuthorization();

// PUT /command endpoint: updates an existing command.
app.MapPut("/command", async (int id, Command updatedCommand, ApplicationDbContext dbContext) =>
{
    logger.LogInformation("Received command update request for ID: {CommandId}", id);
    var command = await dbContext.Commands.FindAsync(id);
    if (command == null)
    {
        logger.LogWarning("Command not found: {CommandId}", id);
        var errorResponse = System.Text.Json.JsonSerializer.Serialize(new { error = "Command not found" });
        return Results.Content(errorResponse, "application/json", statusCode: 404);
    }

    command.CommandText = updatedCommand.CommandText;
    command.Robot = updatedCommand.Robot;
    command.User = updatedCommand.User;
    await dbContext.SaveChangesAsync();

    logger.LogInformation("Command updated successfully: {CommandId}", id);
    var payload = new { message = "Command updated", updatedCommand };
    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(payload);
    return Results.Content(jsonResponse, "application/json");
}).RequireAuthorization();

// GET /command endpoint: fetches a command by ID.
app.MapGet("/command", async (int id, ApplicationDbContext dbContext) =>
{
    logger.LogInformation("Fetching command with ID: {CommandId}", id);
    var command = await dbContext.Commands.FindAsync(id);
    if (command == null)
    {
        logger.LogWarning("Command not found: {CommandId}", id);
        var errorResponse = System.Text.Json.JsonSerializer.Serialize(new { error = "Command not found" });
        return Results.Content(errorResponse, "application/json", statusCode: 404);
    }
    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(command);
    return Results.Content(jsonResponse, "application/json");
}).RequireAuthorization();

// GET /status endpoint: retrieves the robot status.
app.MapGet("/status", async (ApplicationDbContext dbContext) =>
{
    var robotStatus = await dbContext.RobotStatuses.FirstOrDefaultAsync() ?? new RobotStatus("Idle", "0,0", "None");
    logger.LogInformation("Fetched robot status: {Status}", robotStatus.Status);
    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(robotStatus);
    return Results.Content(jsonResponse, "application/json");
}).RequireAuthorization();

// GET /history endpoint: retrieves command history.
app.MapGet("/history", async (ApplicationDbContext dbContext) =>
{
    var commandHistory = await dbContext.Commands.ToListAsync();
    logger.LogInformation("Fetched command history: {CommandCount} commands", commandHistory.Count);
    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(commandHistory);
    return Results.Content(jsonResponse, "application/json");
}).RequireAuthorization();

app.Run();

// Record for user credentials.
public record UserCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Expose Program class for integration testing.
public partial class Program { }
