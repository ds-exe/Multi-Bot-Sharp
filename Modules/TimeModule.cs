using System.Globalization;

namespace Multi_Bot_Sharp.Modules;

public class TimeModule : BaseCommandModule
{
    private DatabaseService _databaseService;
    private Dictionary<string, string> _timeZones;

    private static readonly string _dateRegex = @"^(\d{2})/(\d{2})/?(\d{4})?$";
    private static readonly string _timeRegex = @"\d{2}:\d{2}";

    public TimeModule(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        var text = ConfigHelper.GetJsonText("timezones");
        _timeZones = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
    }

    [Command("time")]
    [Description("Get an embed for a given time/date, optional timezone")]
    public async Task Time(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("optional - region/city or abbreviation")] string? timezone = null)
    {
        if (timezone != null && IsDate(timezone))
        {
            await SendTimeEmbed(ctx, time, timezone, null, TimestampFormat.LongDateTime);
            return;
        }
        await SendTimeEmbed(ctx, time, null, timezone, TimestampFormat.LongDateTime);
    }

    [Command("time")]
    public async Task Time(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("date - dd/mm or dd/mm/yyyy")] string date, [Description("optional - region/city or abbreviation")] string? timezone)
    {
        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.LongDateTime);
    }

    [Command("until")]
    [Description("Get an embed for a given time/date, optional timezone")]
    public async Task Until(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("optional - region/city or abbreviation")] string? timezone = null)
    {
        if (timezone != null && IsDate(timezone))
        {
            await SendTimeEmbed(ctx, time, timezone, null, TimestampFormat.RelativeTime);
            return;
        }
        await SendTimeEmbed(ctx, time, null, timezone, TimestampFormat.RelativeTime);
    }

    [Command("until")]
    public async Task Until(CommandContext ctx, [Description("time - hh:mm")] string time, [Description("date - dd/mm or dd/mm/yyyy")] string date, [Description("optional - region/city or abbreviation")] string? timezone)
    {
        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.RelativeTime);
    }

    [Command("now")]
    [Description("Gets the time for a given timezone")]
    public async Task Now(CommandContext ctx, [Description("Optional timezone, default UTC")] string? timezone = null)
    {
        var zone = GetTimeZone(timezone, ctx.Message.Author.Id);
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
            var timeZoneInfo = _databaseService.GetTimeZone(ctx.Message.Author.Id);
            if (timeZoneInfo != null)
            {
                await ctx.RespondAsync(timeZoneInfo.DisplayName);
            }
            else
            {
                await ctx.RespondAsync("No Time Zone data set");
            }
            return;
        }
        var zone = GetTimeZone(timezone, ctx.Message.Author.Id);
        if (zone != null)
        {
            _databaseService.InsertTimeZone(new TimeZoneData { UserId = ctx.Message.Author.Id, TimeZoneId = zone.Id });
            await ctx.RespondAsync("Time Zone set");
            return;
        }
        await ctx.RespondAsync("Invalid Time Zone");
    }

    private async Task SendTimeEmbed(CommandContext ctx, string time, string? date, string? timezone, TimestampFormat format)
    {
        var zone = GetTimeZone(timezone, ctx.Message.Author.Id);
        if (zone == null)
        {
            await ctx.RespondAsync("Invalid timezone");
            return;
        }
        if (date != null && !IsTime(time) && IsDate(time) && !IsDate(date) && IsTime(date))
        {
            var tmpDate = date;
            date = time;
            time = tmpDate;
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

    private bool IsDate(string date)
    {
        return Regex.Match(date, _dateRegex).Success;
    }

    private bool IsTime(string time)
    {
        return Regex.Match(time, _timeRegex).Success;
    }

    private string? ParseDate(string? date, TimeZoneInfo zone)
    {
        var baseDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        if (date == null)
        {
            return baseDate.ToString("dd/MM/yyyy");
        }
        var matches = Regex.Match(date, _dateRegex);
        if (!matches.Success)
        {
            return null;
        }
        var year = matches.Groups[3].Value;
        return $"{matches.Groups[1].Value}/{matches.Groups[2].Value}/{(year != string.Empty ? year : baseDate.Year)}";
    }

    private TimeZoneInfo? GetTimeZone(string? timezone, ulong uid)
    {
        try
        {
            if (timezone == null)
            {
                var userTimeZone = _databaseService.GetTimeZone(uid);
                if (userTimeZone != null)
                {
                    return userTimeZone;
                }
                return TZConvert.GetTimeZoneInfo("utc");
            }

            var success = _timeZones.TryGetValue(timezone.ToLower(), out var result);
            if (!success || result == null)
            {
                var matches = Regex.Match(timezone.ToLower(), @"^utc(\+|-)([0-9]{1,2})$");
                if (matches.Success)
                {
                    var sign = int.Parse(matches.Groups[2].Value) > 0 ? "-" : "+";
                    var value = Math.Abs(int.Parse(matches.Groups[2].Value));
                    return TZConvert.GetTimeZoneInfo($"Etc/GMT{sign}{value}");
                }

                return TZConvert.GetTimeZoneInfo(timezone);
            }

            return TZConvert.GetTimeZoneInfo(result);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }
}
