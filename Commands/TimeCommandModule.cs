﻿using System.Globalization;

namespace Multi_Bot_Sharp.Commands;

public class TimeCommandModule : ApplicationCommandsModule
{
    public required DatabaseService _databaseService;
    private Dictionary<string, string> _timeZones;

    private static readonly string _dateRegex = @"^(\d{2})/(\d{2})/?(\d{4})?$";
    private static readonly string _timeRegex = @"\d{2}:\d{2}";

    public TimeCommandModule()
    {
        _timeZones = ConfigHelper.GetJsonObject<Dictionary<string, string>>("timezones");
    }

    [SlashCommand("time", "Gets the given time embed.")]
    public async Task TimeCommand(InteractionContext ctx, [Option("time", "Selected Time")] string time, [Option("date", "Selected Date")] string? date = null, [Option("timezone", "Selected Timezone")] string? timezone = null)
    {
        if (!IsTime(time) || (date != null && !IsDate(date)))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Invalid time or date format"
            });
        }

        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.LongDateTime);
    }

    [SlashCommand("until", "Gets the given time embed.")]
    public async Task UntilCommand(InteractionContext ctx, [Option("time", "Selected Time")] string time, [Option("date", "Selected Date")] string? date = null, [Option("timezone", "Selected Timezone")] string? timezone = null)
    {
        if (!IsTime(time) || (date != null && !IsDate(date)))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Invalid time or date format"
            });
        }

        await SendTimeEmbed(ctx, time, date, timezone, TimestampFormat.RelativeTime);
    }

    [SlashCommand("now", "Gets the given time.")]
    public async Task Now(InteractionContext ctx, [Option("timezone", "Selected Timezone")] string? timezone = null)
    {
        var zone = GetTimeZone(timezone, ctx.User.Id);
        if (zone == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Invalid timezone"
            });
            return;
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            Content = $"`{TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).ToString("dd MMM yyyy, HH:mm")}`"
        });
    }

    [SlashCommand("timezone", "Gets / Sets timezone.")]
    [Command("timezone")]
    public async Task TimeZone(InteractionContext ctx, [Option("timezone", "Selected Timezone")] string? timezone = null)
    {
        if (timezone == null)
        {
            var timeZoneInfo = _databaseService.GetTimeZone(ctx.User.Id);
            if (timeZoneInfo != null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = timeZoneInfo.DisplayName
                });
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = "No Time Zone data set"
                });
            }
            return;
        }
        var zone = GetTimeZone(timezone, ctx.User.Id);
        if (zone != null)
        {
            _databaseService.InsertTimeZone(new TimeZoneData { UserId = ctx.User.Id, TimeZoneId = zone.Id });
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Time Zone set"
            });
            return;
        }
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            Content = "Invalid Time Zone"
        });
    }

    private async Task SendTimeEmbed(InteractionContext ctx, string time, string? date, string? timezone, TimestampFormat format)
    {
        var zone = GetTimeZone(timezone, ctx.User.Id);
        if (zone == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Invalid timezone"
            });
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
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Invalid date"
            });
            return;
        }

        var success = DateTime.TryParseExact($"{time} {date}", "HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime);
        if (!success)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Invalid time"
            });
            return;
        }

        var timestamp = TimeZoneInfo.ConvertTimeToUtc(datetime, zone).Timestamp(format);

        var response = new DiscordInteractionResponseBuilder();
        response.AddEmbed(EmbedHelper.GetTimestampEmbed(timestamp));
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
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
                    var sign = int.Parse(matches.Groups[2].Value) > 0 ? "-" : "+"; // Inverted due to Etc/GMT using reversed offsets
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
