﻿namespace Multi_Bot_Sharp.Helpers;

public class EmbedHelper
{
    private static readonly Dictionary<string, string> _resinMap = new Dictionary<string, string>()
        {
            { "hsr", "Honkai: Star Rail "},
            { "genshin", "Genshin" }
        };

    private static string GetGameName(string game)
    {
        return _resinMap[game];
    }

    public static DiscordEmbed GetResinEmbed(ResinData resinData, ResinNotification? nextNotification, int currentResin)
    {
        var embed = new DiscordEmbedBuilder();
        embed.Color = new DiscordColor("0099ff");
        embed.Title = $"{GetGameName(resinData.Game)}";
        embed.Description = $"Current Resin: {currentResin}";
        if (nextNotification != null )
        {
            var utcNotificationTime = DateTime.SpecifyKind(nextNotification.NotificationTimestamp, DateTimeKind.Utc);
            embed.AddField(new DiscordEmbedField("Next alert:", $"{utcNotificationTime.Timestamp()}", true));
            embed.AddField(new DiscordEmbedField("\u200b", "\u200b", true));
        }
        var utcMaxResinTime = DateTime.SpecifyKind(resinData.MaxResinTimestamp, DateTimeKind.Utc);
        embed.AddField(new DiscordEmbedField("Resin full:", utcMaxResinTime.Timestamp(), true));
        return embed.Build();
    }

    public static DiscordComponent[] GetButtons(int customResin)
    {
        return
        [
            new DiscordButtonComponent(ButtonStyle.Secondary, "lowResin", "-10"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "midResin", "-30"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "highResin", "-40")
        ];
    }

    public static DiscordComponent[] GetButtons2(int customResin)
    {
        return
        [
            new DiscordButtonComponent(ButtonStyle.Secondary, "customResin", $"-{customResin}", customResin <= 0),
            new DiscordButtonComponent(ButtonStyle.Secondary, "customResin2", $"-240"),
            new DiscordButtonComponent(ButtonStyle.Primary, "refresh", "↻")
        ];
    }

    public static DiscordEmbed GetTimestampEmbed(string time)
    {
        var embed = new DiscordEmbedBuilder();
        embed.Color = new DiscordColor("00ffff");
        embed.Title = "Local time:";
        embed.Description = time;
        embed.AddField(new DiscordEmbedField("Copy Link:", $"\\{time}"));
        return embed.Build();
    }

    public static DiscordEmbed GetTrackPlayingEmbed(LavalinkTrackInfo trackInfo)
    {
        var embed = new DiscordEmbedBuilder();
        embed.Color = new DiscordColor("0099ff");
        embed.Description = $"Started playing [{trackInfo.Title}]({trackInfo.Uri})";
        return embed.Build();
    }

    public static DiscordEmbed GetTrackAddedEmbed(LavalinkTrackInfo trackInfo, DiscordUser user)
    {
        return GetFormattedTrackEmbed(trackInfo, user, "Added Track");
    }

    public static DiscordEmbed GetNowPlayingEmbed(LavalinkTrackInfo trackInfo, DiscordUser user)
    {
        return GetFormattedTrackEmbed(trackInfo, user, "Currently Playing");
    }

    private static DiscordEmbed GetFormattedTrackEmbed(LavalinkTrackInfo trackInfo, DiscordUser user, string title)
    {
        var embed = new DiscordEmbedBuilder();
        embed.Color = new DiscordColor("0099ff");
        embed.Title = title;
        if (trackInfo.ArtworkUrl != null)
        {
            embed.WithThumbnail(trackInfo.ArtworkUrl);
        }
        embed.Description = $"[{trackInfo.Title}]({trackInfo.Uri})";
        var format = trackInfo.Length.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss";
        embed.AddField(new DiscordEmbedField("Track Length", $"{trackInfo.Length.ToString(format)}", true));
        embed.AddField(new DiscordEmbedField("Added by", $"<@{user.Id}>", true));
        return embed.Build();
    }

    public static DiscordEmbed GetPlaylistAddedEmbed(LavalinkPlaylist playlist, DiscordUser user, string url)
    {
        var embed = new DiscordEmbedBuilder();
        embed.Color = new DiscordColor("0099ff");
        embed.Title = "Added Playlist";
        embed.Description = $"[{playlist.Info.Name}]({url})";
        embed.AddField(new DiscordEmbedField("Playlist Length", $"{playlist.Tracks.Count}", true));
        embed.AddField(new DiscordEmbedField("\u200b", "\u200b", true));
        embed.AddField(new DiscordEmbedField("Added by", $"<@{user.Id}>", true));
        return embed.Build();
    }

    public static DiscordEmbed GetCustomHelpCommandEmbed(CommandContext ctx)
    {
        var command = ctx.Command;
        if (command.Name.ToLower() == "help")
        {
            command = command.Parent;
        }
        var helpCommand = ctx.CommandsNext.FindCommand("help", out var _);
        var commandContext = ctx.CommandsNext.CreateContext(ctx.Message, ctx.Prefix, helpCommand, "perms");
        var customHelpMessage = new CustomFormatHelper(commandContext).WithCommand(command);
        if (command is CommandGroup)
        {
            customHelpMessage.WithSubcommands(((CommandGroup)command).Children);
        }
        return customHelpMessage.Build().Embed;
    }
}
