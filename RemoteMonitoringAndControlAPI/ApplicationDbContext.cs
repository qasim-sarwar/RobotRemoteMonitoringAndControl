using Microsoft.EntityFrameworkCore;

namespace RemoteMonitoringAndControlAPI
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // DbSet for Command entity
        public DbSet<Command> Commands { get; set; }

        // DbSet for Robot entity
        public DbSet<Robot> Robots { get; set; }

        // DbSet for User entity
        public DbSet<User> Users { get; set; }

        // DbSet for Status entity
        public DbSet<RobotStatus> RobotStatuses { get; set; }

        // DbSet for Logs
        public DbSet<Log> Logs { get; set; }
    }

    // Command entity class
    public class Command
    {
        public int Id { get; set; }
        public string CommandText { get; set; }
        public string Robot { get; set; }
        public string User { get; set; }

        // Parameterless constructor (required for object initializer)
        public Command() { }

        // Constructor with parameters (optional, if you want to initialize with values)
        public Command(string commandText, string robot, string user)
        {
            CommandText = commandText;
            Robot = robot;
            User = user;
        }
    }

    // Robot entity class
    public class Robot
    {
        public int Id { get; set; }
        public required string Name { get; set; }         // Marked as required
        public required string Status { get; set; }       // Marked as required

        // Optional constructor to initialize required properties
        public Robot(string name, string status)
        {
            Name = name;
            Status = status;
        }
    }

    // User entity class
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }     // Marked as required
        public required string Password { get; set; }     // Marked as required

        // Optional constructor to initialize required properties
        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    // RobotStatus entity class
    public class RobotStatus
    {
        public int Id { get; set; }
        public string? Status { get; set; }  // Make nullable
        public string? Position { get; set; }  // Make nullable
        public string? Task { get; set; }  // Make nullable

        // Constructor with parameters
        public RobotStatus(string? status, string? position, string? task)
        {
            Status = status;
            Position = position;
            Task = task;
        }
    }

    // Log entity class
    public class Log
    {
        public int Id { get; set; }
        public required string Message { get; set; }      // Marked as required
        public DateTime Timestamp { get; set; }

        // Optional constructor to initialize required properties
        public Log(string message, DateTime timestamp)
        {
            Message = message;
            Timestamp = timestamp;
        }
    }
}
