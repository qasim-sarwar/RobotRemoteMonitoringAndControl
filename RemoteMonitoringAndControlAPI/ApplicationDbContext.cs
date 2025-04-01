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

    //  Command entity class
    public class Command
    {
        public int Id { get; set; }
        public string CommandText { get; set; }
        public string Robot { get; set; }
        public string User { get; set; }
    }

    //  Robot entity class
    public class Robot
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
    }

    //  User entity class
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    //  RobotStatus entity class
    public class RobotStatus
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Position { get; set; }
        public string Task { get; set; }
    }

    ////  Log entity class
    public class Log
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
