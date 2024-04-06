namespace Multi_Bot_Sharp.Modules;

public class TimeModule : BaseCommandModule
{
    private Dictionary<string, string> timezones;

    public TimeModule()
    {
        var text = UtilityModule.GetJsonText("timezones");
        timezones = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
    }

    [Command("time")]
    [Description("Get an embed for a given time/date, optional timezone")]
    public async Task Time(CommandContext ctx, [Description("time - hh:mm")] DateTime datetime, [Description("optional - region/city or abbreviation")] string timezone = "utc")
    {
        await SendTimeEmbed(ctx, datetime, timezone, TimestampFormat.LongDateTime);
    }

    [Command("time")]
    public async Task Time(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("date - dd/mm or dd/mm/yyyy")] string date, [Description("optional - region/city or abbreviation")] string timezone)
    {
        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.LongDateTime);
    }

    [Command("until")]
    [Description("Get an embed for a given time/date, optional timezone")]
    public async Task Until(CommandContext ctx, [Description("time - hh:mm")] DateTime datetime, [Description("optional - region/city or abbreviation")] string timezone = "utc")
    {
        await SendTimeEmbed(ctx, datetime, timezone, TimestampFormat.RelativeTime);
    }

    [Command("until")]
    public async Task Until(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("date - dd/mm or dd/mm/yyyy")] string date, [Description("optional - region/city or abbreviation")] string timezone)
    {
        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.RelativeTime);
    }

    private async Task SendTimeEmbed(CommandContext ctx, DateTime datetime, string timezone, TimestampFormat format)
    {
        var zone = GetTimeZone(timezone);
        if (zone == null)
        {
            await ctx.RespondAsync("Invalid timezone");
            return;
        }

        var timestamp = TimeZoneInfo.ConvertTimeToUtc(datetime, zone).Timestamp(format);
        await ctx.RespondAsync(EmbedModule.GetTimestampEmbed(timestamp));
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

    private async Task SendTimeEmbed(CommandContext ctx, string date, string time, string timezone, TimestampFormat format)
    {
        if (date.Count(c => c == '/') == 1)
        {
            date += "/" + DateTime.UtcNow.Year;
        }
        var success = DateTime.TryParse(time + " " + date, out var datetime);
        if (!success)
        {
            return;
        }

        await SendTimeEmbed(ctx, datetime, timezone, format);
    }

    private TimeZoneInfo GetTimeZone(string timezone)
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
