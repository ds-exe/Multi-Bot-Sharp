using Dapper;
using Microsoft.Data.Sqlite;

namespace Multi_Bot_Sharp.Services;

public class DatabaseService
{
    SqliteConnection _connection;

    public DatabaseService()
    {
        var dbName = Environment.GetEnvironmentVariable("DATABASE_NAME");
        if (dbName == null)
        {
            dbName = "Multi_Bot.db";
        }
        else if (!dbName.Contains(".db"))
        {
            dbName += ".db";
        }
        _connection = new SqliteConnection($"Data Source=Database/{dbName}");
        _connection.Open();
        InitialiseTable("ResinData(userID, game, startResin int, startTimestamp int, resinCapTimestamp int, PRIMARY KEY(userID, game))");
    }

    public void InitialiseTable(string table)
    {
        string query = $"CREATE TABLE IF NOT EXISTS {table}";
        var command = new SqliteCommand(query, _connection);
        command.ExecuteNonQuery();
    }

    private void InsertTest(string table)
    {
        string query = $"INSERT INTO {table} (columns) VALUES (@value)";
        var command = new SqliteCommand(query , _connection);
        command.Parameters.AddWithValue("@value", "value");
        command.ExecuteNonQueryAsync();
    }

    public async Task<Resin?> GetResinData()
    {
        try
        {
            var query = "SELECT * FROM ResinData";
            var results = await _connection.QueryAsync<Resin>(query);
            return results.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
