namespace Multi_Bot_Sharp.Modules;

public class BaseAudioModule : BaseCommandModule
{
    protected QueueService _queueService;

    public BaseAudioModule(QueueService queueService)
    {
        _queueService = queueService;
    }

    protected async Task<bool> JoinAsync(CommandContext ctx)
    {
        var lavalink = ctx.Client.GetLavalink();
        if(!lavalink.ConnectedSessions.Any())
        {
            await ctx.RespondAsync("Music connection error, attempting to reconnect player.");
            await ConfigHelper.ConnectLavalink(lavalink);

            if (!lavalink.ConnectedSessions.Any()) {
                await ctx.RespondAsync("Music connection failed to restart.");
                return false;
            }
        }

        var session = lavalink.ConnectedSessions.Values.First();
        var channel = ctx.Member.VoiceState?.Channel;

        if (channel?.Type != ChannelType.Voice && channel?.Type != ChannelType.Stage)
        {
            await ctx.RespondAsync("Not a valid voice channel.");
            return false;
        }

        await session.ConnectAsync(channel);

        return true;
    }

    protected async Task Play(CommandContext ctx, string? query, bool shuffle)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }

        if (string.IsNullOrEmpty(query))
        {
            await ctx.RespondAsync("No query given.");
            return;
        }

        try
        {
            await ctx.Message.ModifySuppressionAsync(true);
        }
        catch { }

        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel?.Id == null)
        {
            await ctx.RespondAsync("You are not in a voice channel.");
            return;
        }

        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);

        if (guildPlayer == null)
        {
            if (!JoinAsync(ctx).Result)
            {
                return;
            }
            guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
            if (guildPlayer == null)
            {
                return;
            }
        }

        var type = LavalinkSearchType.Youtube;
        if (query.ToLower().Contains("http"))
        {
            type = LavalinkSearchType.Plain;
        }

        var loadResult = await guildPlayer.LoadTracksAsync(type, query);

        // If something went wrong on Lavalink's end or it just couldn't find anything.
        if (loadResult.LoadType == LavalinkLoadResultType.Empty || loadResult.LoadType == LavalinkLoadResultType.Error)
        {
            await ctx.RespondAsync($"Track search failed for {query}.");
            return;
        }

        var queue = _queueService.AddQueue(guildPlayer.ChannelId, guildPlayer);

        if (loadResult.LoadType == LavalinkLoadResultType.Track)
        {
            QueueTrack(ctx, queue, loadResult.GetResultAs<LavalinkTrack>());
        }
        else if (loadResult.LoadType == LavalinkLoadResultType.Playlist)
        {
            QueuePlaylist(ctx, queue, loadResult.GetResultAs<LavalinkPlaylist>(), query);
        }
        else if (loadResult.LoadType == LavalinkLoadResultType.Search)
        {
            QueueTrack(ctx, queue, loadResult.GetResultAs<List<LavalinkTrack>>().First());
        }
        else
        {
            throw new InvalidOperationException("Unexpected load result type.");
        }

        if (shuffle)
        {
            await Shuffle(ctx);
        }
        _ = queue.PlayQueueAsync();
    }

    protected async Task Shuffle(CommandContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null)
        {
            await ctx.RespondAsync("Nothing is playing.");
            return;
        }

        var queue = _queueService.GetQueue(guildPlayer.ChannelId);

        queue?.Shuffle();
    }

    protected void QueueTrack(CommandContext ctx, Queue queue, LavalinkTrack track)
    {
        ctx.Channel.SendMessageAsync(EmbedHelper.GetTrackAddedEmbed(track.Info, ctx.User));
        queue.AddTrack(ctx, track);
    }

    protected void QueuePlaylist(CommandContext ctx, Queue queue, LavalinkPlaylist playlist, string url)
    {
        ctx.Channel.SendMessageAsync(EmbedHelper.GetPlaylistAddedEmbed(playlist, ctx.User, url));
        foreach (var track in playlist.Tracks)
        {
            queue.AddTrack(ctx, track);
        }
    }

    public async Task Play(CommandContext ctx, [RemainingText] string? query)
    {
        await Play(ctx, query, false);
    }

    public async Task Shuffle(CommandContext ctx, [RemainingText] string? query)
    {
        if (query == null)
        {
            await Shuffle(ctx);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            return;
        }
        await Play(ctx, query, true);
    }

    public async Task Skip(CommandContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null || guildPlayer.CurrentTrack == null)
        {
            await ctx.RespondAsync("Nothing is playing.");
            return;
        }

        await guildPlayer.StopAsync();
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
    }

    public async Task Stop(CommandContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null || guildPlayer.CurrentTrack == null)
        {
            await ctx.RespondAsync("Nothing is playing.");
            return;
        }

        _queueService.RemoveQueue(guildPlayer.ChannelId);
        await guildPlayer.StopAsync();
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
    }

    public async Task Leave(CommandContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null)
        {
            await ctx.RespondAsync("Not in voice.");
            return;
        }

        await guildPlayer.DisconnectAsync();
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
    }

    public async Task NowPlaying(CommandContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null || guildPlayer.CurrentTrack == null)
        {
            await ctx.RespondAsync("Nothing is playing.");
            return;
        }
        var user = _queueService.GetQueue(guildPlayer.ChannelId)?.GetCurrentTrackUser();
        if (user == null)
        {
            await ctx.RespondAsync("Nothing is playing.");
            return;
        }

        await ctx.Channel.SendMessageAsync(EmbedHelper.GetNowPlayingEmbed(guildPlayer.CurrentTrack.Info, user));
    }
}

[Group("music")]
[Description("Music commands")]
public class AudioModule : BaseAudioModule
{
    public AudioModule(QueueService queueService) : base(queueService) { }

    [GroupCommand, Command("help")]
    [Description("Lists music commands")]
    public async Task Help(CommandContext ctx)
    {
        await ctx.RespondAsync(EmbedHelper.GetCustomHelpCommandEmbed(ctx));
    }

    [Command("play")]
    [Description("Plays music")]
    public new async Task Play(CommandContext ctx, [RemainingText] string? query)
    {
        await base.Play(ctx, query);
    }

    [Command("shuffle")]
    [Description("Shuffles current queue, can use to initate shuffled playback")]
    public new async Task Shuffle(CommandContext ctx, [RemainingText] string? query)
    {
        await base.Shuffle(ctx, query);
    }

    [Command("skip")]
    [Description("Skips track")]
    public new async Task Skip(CommandContext ctx)
    {
        await base.Skip(ctx);
    }

    [Command("stop")]
    [Description("Stops playback")]
    public new async Task Stop(CommandContext ctx)
    {
        await base.Stop(ctx);
    }

    [Command("leave")]
    [Description("Leaves channel")]
    public new async Task Leave(CommandContext ctx)
    {
        await base.Leave(ctx);
    }

    [Command("nowplaying")]
    [Description("Shows currently playing song")]
    public new async Task NowPlaying(CommandContext ctx)
    {
        await base.NowPlaying(ctx);
    }
}

[Hidden]
public class AudioShorthandModule : BaseAudioModule
{
    public AudioShorthandModule(QueueService queueService) : base(queueService) { }

    [Command("play")]
    [Description("Plays music")]
    public new async Task Play(CommandContext ctx, [RemainingText] string? query)
    {
        await base.Play(ctx, query);
    }

    [Command("shuffle")]
    [Description("Shuffles current queue, can use to initate shuffled playback")]
    public new async Task Shuffle(CommandContext ctx, [RemainingText] string? query)
    {
        await base.Shuffle(ctx, query);
    }

    [Command("skip")]
    [Description("Skips track")]
    public new async Task Skip(CommandContext ctx)
    {
        await base.Skip(ctx);
    }

    [Command("stop")]
    [Description("Stops playback")]
    public new async Task Stop(CommandContext ctx)
    {
        await base.Stop(ctx);
    }

    [Command("leave")]
    [Description("Leaves channel")]
    public new async Task Leave(CommandContext ctx)
    {
        await base.Leave(ctx);
    }

    [Command("nowplaying")]
    [Description("Shows currently playing song")]
    public new async Task NowPlaying(CommandContext ctx)
    {
        await base.NowPlaying(ctx);
    }
}
