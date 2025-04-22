using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMonitoringAndControlAPI;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        _client = _factory.CreateClient();
    }

    // Test: Login returns JWT token for valid credentials
    [Fact]
    public async Task Login_ReturnsJwtToken_OnValidCredentials()
    {
        var credentials = new UserCredentials { Username = "user", Password = "password" };
        var response = await _client.PostAsJsonAsync("/login", credentials);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", content);
    }

    // Test: Login fails for invalid credentials
    [Fact]
    public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
    {
        var credentials = new UserCredentials { Username = "invalid", Password = "wrongpassword" };
        var response = await _client.PostAsJsonAsync("/login", credentials);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Health check endpoint
    [Fact]
    public async Task HealthCheck_ReturnsOkStatus()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    // Test: Command Create returns success
    [Fact]
    public async Task Command_Create_ReturnsSuccess()
    {
        // Get JWT token
        var credentials = new UserCredentials { Username = "user", Password = "password" };
        var loginResponse = await _client.PostAsJsonAsync("/login", credentials);
        loginResponse.EnsureSuccessStatusCode();

        var content = await loginResponse.Content.ReadAsStringAsync();
        var token = content.Split('"')[3]; // Extract token from response

        // Attach JWT token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Now make the command request
        var command = new Command { CommandText = "MoveForward", Robot = "Robot1", User = "user" };
        var response = await _client.PostAsJsonAsync("/command", command);

        response.EnsureSuccessStatusCode();
    }

    // Test: Command Get returns the created command with authentication
    [Fact]
    public async Task Command_Get_ReturnsCommand_WithAuthentication()
    {
        // Authenticate and get the JWT token
        var loginResponse = await _client.PostAsJsonAsync("/login", new
        {
            Username = "user",
            Password = "password"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = loginResult.GetProperty("token").GetString();

        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a command using the POST endpoint
        var newCommand = new Command
        {
            CommandText = "MoveForward",
            Robot = "Robot1",
            User = "user"
        };
        var postResponse = await _client.PostAsJsonAsync("/command", newCommand);
        postResponse.EnsureSuccessStatusCode();
        var postResult = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        int commandId = postResult.GetProperty("commandId").GetInt32();

        // Retrieve the created command using the GET endpoint
        var getResponse = await _client.GetAsync($"/command?id={commandId}");
        getResponse.EnsureSuccessStatusCode();

        // Assert the response content
        var commandResult = await getResponse.Content.ReadFromJsonAsync<Command>();
        Assert.Equal("MoveForward", commandResult.CommandText);
    }

    // Test: Command Create with in-memory DB (direct DbContext usage)
    [Fact]
    public async Task Command_Create_ReturnsSuccess_WithInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using (var context = new ApplicationDbContext(options))
        {
            var command = new Command { CommandText = "MoveForward", Robot = "Robot1", User = "user" };

            // Act
            context.Commands.Add(command);
            await context.SaveChangesAsync();

            // Assert
            var savedCommand = await context.Commands.FirstOrDefaultAsync(c => c.CommandText == "MoveForward");
            Assert.NotNull(savedCommand);
            Assert.Equal("MoveForward", savedCommand.CommandText);
        }
    }

    // Test: Command Create with valid JWT token
    [Fact]
    public async Task Command_Create_ReturnsSuccess_WithValidToken()
    {
        var credentials = new UserCredentials { Username = "user", Password = "password" };
        var response = await _client.PostAsJsonAsync("/login", credentials);
        var content = await response.Content.ReadAsStringAsync();
        var token = content.Split('"')[3]; // Extract token from response

        // Add JWT token to Authorization header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new Command { CommandText = "MoveForward", Robot = "Robot1", User = "user" };
        response = await _client.PostAsJsonAsync("/command", command);

        response.EnsureSuccessStatusCode();
    }

    // Test: Command Get with valid JWT token
    [Fact]
    public async Task Command_Get_ReturnsCommand_WithValidToken()
    {
        var credentials = new UserCredentials { Username = "user", Password = "password" };
        var response = await _client.PostAsJsonAsync("/login", credentials);
        var content = await response.Content.ReadAsStringAsync();
        var token = content.Split('"')[3]; // Extract token from response

        // Add JWT token to Authorization header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var commandId = 1; // Replace with a valid command ID from the in-memory DB
        response = await _client.GetAsync($"/command?id={commandId}");

        response.EnsureSuccessStatusCode();
    }

    // Test: Status endpoint returns a robot status with authentication
    [Fact]
    public async Task Status_ReturnsRobotStatus_WithAuthentication()
    {
        // Authenticate and get the JWT token
        var loginResponse = await _client.PostAsJsonAsync("/login", new
        {
            Username = "user",
            Password = "password"
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = loginResult.GetProperty("token").GetString();

        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Call the /status endpoint with authentication
        var response = await _client.GetAsync("/status");
        response.EnsureSuccessStatusCode();
    }

    // Test: Command history returns list of commands with authentication
    [Fact]
    public async Task History_ReturnsCommandHistory_WithAuthentication()
    {
        // Authenticate and get the JWT token
        var loginResponse = await _client.PostAsJsonAsync("/login", new
        {
            Username = "user",
            Password = "password"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = loginResult.GetProperty("token").GetString();

        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Call the history endpoint
        var response = await _client.GetAsync("/history");
        response.EnsureSuccessStatusCode();
    }

    // Additional Tests

    // Test: Command Update returns success with valid JWT token
    [Fact]
    public async Task Command_Update_ReturnsSuccess_WithValidToken()
    {
        // Authenticate and get JWT token
        var loginResponse = await _client.PostAsJsonAsync("/login", new { Username = "user", Password = "password" });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = loginResult.GetProperty("token").GetString();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a command to update
        var newCommand = new Command { CommandText = "MoveForward", Robot = "Robot1", User = "user" };
        var postResponse = await _client.PostAsJsonAsync("/command", newCommand);
        postResponse.EnsureSuccessStatusCode();
        var postResult = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        int commandId = postResult.GetProperty("commandId").GetInt32();

        // Update the command using the PUT endpoint
        var updatedCommand = new Command { CommandText = "TurnLeft", Robot = "Robot1", User = "user" };
        var updateResponse = await _client.PutAsJsonAsync($"/command?id={commandId}", updatedCommand);
        updateResponse.EnsureSuccessStatusCode();

        // Verify the update by retrieving the command
        var getResponse = await _client.GetAsync($"/command?id={commandId}");
        getResponse.EnsureSuccessStatusCode();
        var commandResult = await getResponse.Content.ReadFromJsonAsync<Command>();
        Assert.Equal("TurnLeft", commandResult.CommandText);
    }

    // Test: Command Update returns NotFound for non-existent command
    [Fact]
    public async Task Command_Update_ReturnsNotFound_ForNonExistentCommand()
    {
        // Authenticate and get JWT token
        var loginResponse = await _client.PostAsJsonAsync("/login", new { Username = "user", Password = "password" });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = loginResult.GetProperty("token").GetString();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Attempt to update a non-existent command
        int nonExistentCommandId = 9999;
        var updatedCommand = new Command { CommandText = "TestUpdate", Robot = "Robot1", User = "user" };
        var response = await _client.PutAsJsonAsync($"/command?id={nonExistentCommandId}", updatedCommand);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test: Command Get returns NotFound for non-existent command with authentication
    [Fact]
    public async Task Command_Get_ReturnsNotFound_ForNonExistentCommand_WithAuthentication()
    {
        // Authenticate and get JWT token
        var loginResponse = await _client.PostAsJsonAsync("/login", new { Username = "user", Password = "password" });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        string token = loginResult.GetProperty("token").GetString();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        int nonExistentCommandId = 9999;
        var response = await _client.GetAsync($"/command?id={nonExistentCommandId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test: Command Create returns Unauthorized without token
    [Fact]
    public async Task Command_Create_ReturnsUnauthorized_WithoutToken()
    {
        // Ensure no token is set
        _client.DefaultRequestHeaders.Authorization = null;
        var command = new Command { CommandText = "Test", Robot = "Robot1", User = "user" };
        var response = await _client.PostAsJsonAsync("/command", command);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Command Update returns Unauthorized without token
    [Fact]
    public async Task Command_Update_ReturnsUnauthorized_WithoutToken()
    {
        // Ensure no token is set
        _client.DefaultRequestHeaders.Authorization = null;
        var updatedCommand = new Command { CommandText = "TestUpdate", Robot = "Robot1", User = "user" };
        var response = await _client.PutAsJsonAsync("/command?id=1", updatedCommand);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Command Get returns Unauthorized without token
    [Fact]
    public async Task Command_Get_ReturnsUnauthorized_WithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/command?id=1");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Status endpoint returns Unauthorized without token
    [Fact]
    public async Task Status_ReturnsUnauthorized_WithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/status");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: History endpoint returns Unauthorized without token
    [Fact]
    public async Task History_ReturnsUnauthorized_WithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/history");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

}
