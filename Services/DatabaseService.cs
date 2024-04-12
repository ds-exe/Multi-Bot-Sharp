using Dapper;
using Microsoft.Data.Sqlite;
using Octokit;
using SQLitePCL;

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
        try
        {
            _connection.Open();
            InitialiseTables();
        }
        catch
        {
            Console.WriteLine("DB connection error, folder or name error");
        }
    }

    public void InitialiseTables()
    {
        InitialiseTable("TimeZones(UserId integer PRIMARY KEY, TimeZoneDisplayName)");
        InitialiseTable("ResinData(UserId, Game, StartResin int, StartTimestamp int, ResinCapTimestamp int, PRIMARY KEY(UserId, Game))");
    }

    public void InitialiseTable(string table)
    {
        string query = $"CREATE TABLE IF NOT EXISTS {table}";
        _connection.Execute(query);
    }

    public void InsertTimeZone(TimeZoneData tz)
    {
        try
        {
            string query = $"REPLACE INTO Timezones (UserId, TimeZoneDisplayName) VALUES (@UserId, @TimeZoneDisplayName)";
            _connection.Execute(query, tz);
        }
        catch
        {

        }
    }

    public TimeZoneData? GetTimeZone(ulong UserId)
    {
        try
        {
            string query = $"SELECT * FROM Timezones WHERE UserId = @UserId";
            return _connection.Query<TimeZoneData>(query, new { UserId }).FirstOrDefault();
        }
        catch
        {
            return null;
        }
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
