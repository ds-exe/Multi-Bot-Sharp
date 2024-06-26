﻿using Dapper;
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
        try
        {
            _connection.Open();
            InitialiseTables();
        }
        catch
        {
            Console.WriteLine("DB connection error, folder or name invalid");
        }
    }

    public void InitialiseTables()
    {
        InitialiseTable("TimeZoneData(UserId INTEGER PRIMARY KEY, TimeZoneId TEXT)");
        InitialiseTable("ResinData(UserId INTEGER, Game TEXT, MaxResinTimestamp INTEGER, PRIMARY KEY(UserId, Game))");
        InitialiseTable("ResinNotification(UserId INTEGER, Game TEXT, NotificationTimestamp INTEGER, " +
            "MaxResinTimestamp INTEGER, PRIMARY KEY(UserId, Game, NotificationTimestamp))");
        InitialiseTable("CustomResinData(UserId INTEGER, Game TEXT, Resin INTEGER, PRIMARY KEY(UserId, Game))");
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
            string query = $"REPLACE INTO TimeZoneData (UserId, TimeZoneId) VALUES (@UserId, @TimeZoneId)";
            _connection.Execute(query, tz);
        }
        catch { }
    }

    public TimeZoneInfo? GetTimeZone(ulong userId)
    {
        try
        {
            string query = $"SELECT * FROM TimeZoneData WHERE UserId = @userId";
            var data = _connection.Query<TimeZoneData>(query, new { userId }).FirstOrDefault();
            if (data == null)
            {
                return null;
            }
            return TZConvert.GetTimeZoneInfo(data.TimeZoneId);
        }
        catch
        {
            return TZConvert.GetTimeZoneInfo("utc");
        }
    }

    public void InsertResinData(ResinData resinData)
    {
        try
        {
            string query = $"REPLACE INTO ResinData (UserId, Game, MaxResinTimestamp) VALUES (@UserId, @Game, @MaxResinTimestamp)";
            _connection.Execute(query, resinData);
        }
        catch { }
    }

    public ResinData? GetResinData(ulong userId, string game)
    {
        try
        {
            var query = "SELECT * FROM ResinData WHERE UserId = @userId AND Game = @game";
            return _connection.Query<ResinData>(query, new { userId, game }).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public void InsertResinNotification(ResinNotification resinNotification)
    {
        try
        {
            string query = $"REPLACE INTO ResinNotification (UserId, Game, NotificationTimestamp, MaxResinTimestamp) VALUES " +
                $"(@UserId, @Game, @NotificationTimestamp, @MaxResinTimestamp)";
            _connection.Execute(query, resinNotification);
        }
        catch { }
    }

    public void ClearOldResinNotifications(ulong userId, string game)
    {
        try
        {
            string query = $"DELETE FROM ResinNotification WHERE UserId = @userId AND Game = @game";
            _connection.Execute(query, new { userId, game });
        }
        catch { }
    }

    public IEnumerable<ResinNotification> GetElapsedResinNotifications(DateTime timeNow)
    {
        try
        {
            string query = $"SELECT * FROM ResinNotification WHERE NotificationTimestamp <= @timeNow";
            return _connection.Query<ResinNotification>(query, new { timeNow });
        }
        catch
        {
            return new List<ResinNotification>();
        }
    }

    public ResinNotification? GetNextResinNotification(ResinData resinData)
    {
        try
        {
            string query = $"SELECT * FROM ResinNotification WHERE UserId = @UserId AND Game = @Game";
            var notifications = _connection.Query<ResinNotification>(query, resinData);
            var response = notifications.FirstOrDefault();
            foreach (var notification in notifications)
            {
                if (notification.NotificationTimestamp < response?.NotificationTimestamp)
                {
                    response = notification;
                }
            }
            return response;
        }
        catch { }
        return null;
    }

    public void DeleteElapsedResinNotifications(DateTime timeNow)
    {
        try
        {
            string query = $"DELETE FROM ResinNotification WHERE NotificationTimestamp <= @timeNow";
            _connection.Execute(query, new { timeNow });
        }
        catch { }
    }

    public void InsertCustomResinData(CustomResinData customResinData)
    {
        try
        {
            string query = $"REPLACE INTO CustomResinData (UserId, Game, Resin) VALUES (@UserId, @Game, @Resin)";
            _connection.Execute(query, customResinData);
        }
        catch { }
    }

    public CustomResinData? GetCustomResinData(ulong userId, string game)
    {
        try
        {
            var query = "SELECT * FROM CustomResinData WHERE UserId = @userId AND Game = @game";
            return _connection.Query<CustomResinData>(query, new { userId, game }).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
