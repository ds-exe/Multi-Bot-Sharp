using System.Globalization;

namespace Multi_Bot_Sharp.Modules;

public class TimeModule : BaseCommandModule
{
    private Dictionary<string, string> timezones;

    public TimeModule()
    {
        var text = ConfigHelper.GetJsonText("timezones");
        timezones = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
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

    private async Task SendTimeEmbed(CommandContext ctx, string time, string? date, string timezone, TimestampFormat format)
    {
        var zone = GetTimeZone(timezone);
        if (zone == null)
        {
            await ctx.RespondAsync("Invalid timezone");
            return;
        }
        if (date == null)
        {
            date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).ToString("dd/MM/yyyy");
        }
        if (date.Count(c => c == '/') == 1)
        {
            date += "/" + TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Year;
        }
        var success = DateTime.TryParseExact(time + " " + date, "HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime);
        if (!success)
        {
            return;
        }

        var timestamp = TimeZoneInfo.ConvertTimeToUtc(datetime, zone).Timestamp(format);
        await ctx.RespondAsync(EmbedHelper.GetTimestampEmbed(timestamp));
    }

    private TimeZoneInfo? GetTimeZone(string timezone)
    {
        try
        {
            var success = timezones.TryGetValue(timezone, out var result);
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
