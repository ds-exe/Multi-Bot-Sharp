using System.Globalization;

namespace Multi_Bot_Sharp.Modules;

public class TimeModule : BaseCommandModule
{
    private DatabaseService _databaseService;
    private Dictionary<string, string> _timeZones;

    public TimeModule(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        var text = ConfigHelper.GetJsonText("TimeZones");
        _timeZones = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
    }

    [Command("time")]
    [Description("Get an embed for a given time/date, optional timezone")]
    public async Task Time(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("optional - region/city or abbreviation")] string timezone = "utc")
    {
        await SendTimeEmbed(ctx, time, null, timezone, TimestampFormat.LongDateTime);
    }

    [Command("time")]
    public async Task Time(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("date - dd/mm or dd/mm/yyyy")] string date, [Description("region/city or abbreviation")] string timezone)
    {
        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.LongDateTime);
    }

    [Command("until")]
    [Description("Get an embed for a given time/date, optional timezone")]
    public async Task Until(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("optional - region/city or abbreviation")] string timezone = "utc")
    {
        await SendTimeEmbed(ctx, time, null, timezone, TimestampFormat.RelativeTime);
    }

    [Command("until")]
    public async Task Until(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("date - dd/mm or dd/mm/yyyy")] string date, [Description("region/city or abbreviation")] string timezone)
    {
        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.RelativeTime);
    }

    [Command("now")]
    [Description("Gets the time for a given timezone")]
    public async Task Now(CommandContext ctx, [Description("Optional timezone, default UTC")] string timezone = "utc")
    {
        var zone = GetTimeZone(timezone);
        if (zone == null)
        {
            await ctx.RespondAsync("Invalid timezone");
            return;
        }

        await ctx.RespondAsync($"`{TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).ToString("dd MMM yyyy, HH:mm")}`");
    }

    [Command("timezone")]
    public async Task TimeZone(CommandContext ctx, string? timezone = null)
    {
        if (timezone == null)
        {
            var timeZoneData = _databaseService.GetTimeZone(ctx.Member.Id);
            if (timeZoneData != null)
            {
                await ctx.RespondAsync(timeZoneData.TimeZoneDisplayName);
            }
            else
            {
                await ctx.RespondAsync("No Time Zone data set");
            }
            return;
        }
        var zone = GetTimeZone(timezone);
        if (zone != null)
        {
            _databaseService.InsertTimeZone(new TimeZoneData { UserId = ctx.Member.Id, TimeZoneDisplayName = zone.DisplayName });
            await ctx.RespondAsync("Time Zone set");
            return;
        }
        await ctx.RespondAsync("Invalid Time Zone");
    }

    private async Task SendTimeEmbed(CommandContext ctx, string time, string? date, string timezone, TimestampFormat format)
    {
        var zone = GetTimeZone(timezone);
        if (zone == null)
        {
            await ctx.RespondAsync("Invalid timezone");
            return;
        }

        date = ParseDate(date, zone);
        if (date == null)
        {
            await ctx.RespondAsync("Invalid date");
            return;
        }

        var success = DateTime.TryParseExact($"{time} {date}", "HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime);
        if (!success)
        {
            await ctx.RespondAsync("Invalid time");
            return;
        }

        var timestamp = TimeZoneInfo.ConvertTimeToUtc(datetime, zone).Timestamp(format);
        await ctx.RespondAsync(EmbedHelper.GetTimestampEmbed(timestamp));
    }

    private string? ParseDate(string? date, TimeZoneInfo zone)
    {
        var baseDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        if (date == null)
        {
            return baseDate.ToString("dd/MM/yyyy");
        }
        var matches = Regex.Match(date, @"^(\d{2})/(\d{2})/?(\d{4})?$");
        if (!matches.Success)
        {
            return null;
        }
        var year = matches.Groups[3].Value;
        return $"{matches.Groups[1].Value}/{matches.Groups[2].Value}/{(year != string.Empty ? year : baseDate.Year)}"; ;
    }

    private TimeZoneInfo? GetTimeZone(string timezone)
    {
        try
        {
            var success = _timeZones.TryGetValue(timezone, out var result);
            if (!success || result == null)
            {
                return TZConvert.GetTimeZoneInfo(timezone);
            }

            return TZConvert.GetTimeZoneInfo(result);
        }
        catch (TimeZoneNotFoundException e)
        {
            return null;
        }
    }

    public static string GenerateUnixTimeNow()
    {
        return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
