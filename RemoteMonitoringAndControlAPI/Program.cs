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
using System.Text.Json;

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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Robot Control API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' in the field below"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Define default JSON serialization options.
JsonSerializerOptions jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

// Health check endpoint
app.MapGet("/health", async (HttpContext context) =>
{
    logger.LogInformation("Health check called");
    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, "API is healthy", jsonOptions);
});

// Login endpoint for JWT token generation
app.MapPost("/login", async (HttpContext context) =>
{
    var credentials = await context.Request.ReadFromJsonAsync<UserCredentials>();
    logger.LogInformation("Login attempt by user: {Username}", credentials?.Username);

    if (credentials is null)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = "Credentials are null" }, jsonOptions);
        return;
    }

    await Task.Yield();

    if (credentials.Username == "user" && credentials.Password == "password")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, credentials.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { token = jwt }, jsonOptions);
    }
    else
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = "Unauthorized" }, jsonOptions);
    }
});

// POST /command endpoint: creates a new command.
app.MapPost("/command", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    var command = await context.Request.ReadFromJsonAsync<Command>();
    try
    {
        dbContext.Commands.Add(command);
        await dbContext.SaveChangesAsync();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { message = "Command accepted", commandId = command.Id }, jsonOptions);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /command endpoint");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = ex.Message }, jsonOptions);
    }
}).RequireAuthorization();

// PUT /command endpoint: updates an existing command.
app.MapPut("/command", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    if (!context.Request.Query.TryGetValue("id", out var idValue) || !int.TryParse(idValue, out int id))
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = "Invalid or missing command id" }, jsonOptions);
        return;
    }

    var updatedCommand = await context.Request.ReadFromJsonAsync<Command>();
    logger.LogInformation("Received command update request for ID: {CommandId}", id);
    var command = await dbContext.Commands.FindAsync(id);
    if (command is null)
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = "Command not found" }, jsonOptions);
        return;
    }

    command.CommandText = updatedCommand.CommandText;
    command.Robot = updatedCommand.Robot;
    command.User = updatedCommand.User;
    await dbContext.SaveChangesAsync();

    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, new { message = "Command updated", updatedCommand }, jsonOptions);
}).RequireAuthorization();

// GET /command endpoint: fetches a command by ID.
app.MapGet("/command", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    if (!context.Request.Query.TryGetValue("id", out var idValue) || !int.TryParse(idValue, out int id))
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = "Invalid or missing command id" }, jsonOptions);
        return;
    }

    logger.LogInformation("Fetching command with ID: {CommandId}", id);
    var command = await dbContext.Commands.FindAsync(id);
    if (command is null)
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { error = "Command not found" }, jsonOptions);
        return;
    }

    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, command, jsonOptions);
}).RequireAuthorization();

// GET /status endpoint: retrieves the robot status.
app.MapGet("/status", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    var robotStatus = await dbContext.RobotStatuses.FirstOrDefaultAsync() ?? new RobotStatus("Idle", "0,0", "None");
    logger.LogInformation("Fetched robot status: {Status}", robotStatus.Status);
    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, robotStatus, jsonOptions);
}).RequireAuthorization();

// GET /history endpoint: retrieves command history.
app.MapGet("/history", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    var commandHistory = await dbContext.Commands.ToListAsync();
    logger.LogInformation("Fetched command history: {CommandCount} commands", commandHistory.Count);
    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, commandHistory, jsonOptions);
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
