using System.Data;
using System.Text.Json;
using Npgsql;

namespace CommunityHub.Application.Database;

public static class PostgresConnection
{
    private static readonly string ConnectionString = LoadConnectionString();

    /// <summary>Returns the connection string from appsettings.json. Use this everywhere instead of hardcoding.</summary>
    public static string GetConnectionString() => ConnectionString;

    public static IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    private static string LoadConnectionString()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found at: {configPath}");
        }

        var json = File.ReadAllText(configPath);
        using var document = JsonDocument.Parse(json);

        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
        }

        return connectionString;
    }
}
