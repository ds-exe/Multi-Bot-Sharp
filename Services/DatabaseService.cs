using Dapper;
using Microsoft.Data.Sqlite;

namespace Multi_Bot_Sharp.Services;

public class DatabaseService
{
    SqliteConnection _connection;

    public DatabaseService()
    {
        _connection = new SqliteConnection($"Data Source=Multi_Bot.db");
        _connection.QueryAsync("CREATE TABLE IF NOT EXISTS ResinData(userID, game, startResin int, startTimestamp int, resinCapTimestamp int, PRIMARY KEY (userID, game))");
    }

    public async Task<Resin> GetResinData()
    {
        try
        {
            var query = "SELECT * FROM ResinData";
            var results = await _connection.QueryAsync<Resin>(query);
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            return null;
        }

    }
}
