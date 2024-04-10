namespace Multi_Bot_Sharp.Modules;

[Group("music")]
[Description("Music commands")]
public class AudioModule : BaseCommandModule
{
    private const int timeoutMinutes = 15;

    private QueueService _queueService;

    public AudioModule(QueueService queueService)
    {
        _queueService = queueService;
    }

    [GroupCommand, Command("help")]
    [Description("Lists music commands")]
    public async Task Help(CommandContext ctx)
    {
        await ctx.RespondAsync(UtilityModule.GetCustomHelpCommand(ctx));
    }

    private async Task<bool> JoinAsync(CommandContext ctx)
    {
        var lavalink = ctx.Client.GetLavalink();
        if(!lavalink.ConnectedSessions.Any())
        {
            await ctx.RespondAsync("Music connection error, attempting to reconnect player.");
            await UtilityModule.ConnectLavalink(lavalink);

            if (!lavalink.ConnectedSessions.Any()) {
                await ctx.RespondAsync("Music connection failed to restart.");
                return false;
            }
        }

        var session = lavalink.ConnectedSessions.Values.First();
        var channel = ctx.Member.VoiceState.Channel;

        if (channel.Type != ChannelType.Voice && channel.Type != ChannelType.Stage)
        {
            await ctx.RespondAsync("Not a valid voice channel.");
            return false;
        }

        var player = await session.ConnectAsync(channel);

        player.TrackEnded += Player_TrackEnded;
        return true;
    }

    [Command("play")]
    [Description("Plays music")]
    public async Task Play(CommandContext ctx, [RemainingText] string query)
    {
        if (UtilityModule.IsDM(ctx))
        {
            await ctx.RespondAsync("Cannot play in DM's.");
            return;
        }

        if (string.IsNullOrEmpty(query))
        {
            await ctx.RespondAsync("No query given.");
            return;
        }

        await ctx.Message.ModifySuppressionAsync(true);

        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
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

        var queue = _queueService.GetQueue(guildPlayer.ChannelId);

        if (loadResult.LoadType == LavalinkLoadResultType.Track)
        {
            QueueTrack(ctx, queue, loadResult.GetResultAs<LavalinkTrack>());
        }
        else if(loadResult.LoadType == LavalinkLoadResultType.Playlist)
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

        PlayQueueAsync(guildPlayer, queue);
    }

    [Command("skip")]
    [Description("Skips track")]
    public async Task Skip(CommandContext ctx)
    {
        if (UtilityModule.IsDM(ctx))
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

    [Command("stop")]
    [Description("Stops playback")]
    public async Task Stop(CommandContext ctx)
    {
        if (UtilityModule.IsDM(ctx))
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

    [Command("leave")]
    [Description("Leaves channel")]
    public async Task Leave(CommandContext ctx)
    {
        if (UtilityModule.IsDM(ctx))
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

    [Command("nowplaying")]
    [Description("Shows currently playing song")]
    public async Task NowPlaying(CommandContext ctx)
    {
        if (UtilityModule.IsDM(ctx))
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

        await ctx.Channel.SendMessageAsync(EmbedModule.GetNowPlayingEmbed(guildPlayer.CurrentTrack.Info, ctx.User));
    }

    private void QueueTrack(CommandContext ctx, Queue queue, LavalinkTrack track)
    {
        ctx.Channel.SendMessageAsync(EmbedModule.GetTrackAddedEmbed(track.Info, ctx.User));
        queue.AddTrack(ctx.Channel, track);
    }

    private void QueuePlaylist(CommandContext ctx, Queue queue, LavalinkPlaylist playlist, string url)
    {
        ctx.Channel.SendMessageAsync(EmbedModule.GetPlaylistAddedEmbed(playlist, ctx.User, url));
        foreach (var track in playlist.Tracks)
        {
            queue.AddTrack(ctx.Channel, track);
        }
    }

    private async Task Player_TrackEnded(LavalinkGuildPlayer sender, DisCatSharp.Lavalink.EventArgs.LavalinkTrackEndedEventArgs e)
    {
        await PlayQueueAsync(sender, _queueService.GetQueue(sender.ChannelId));
    }

    public async Task PlayQueueAsync(LavalinkGuildPlayer player, Queue queue)
    {
        if (queue == null)
        {
            return;
        }

        if (player.CurrentTrack != null)
        {
            return;
        }

        if (queue.PreviousQueueEntry != null)
        {
            try
            {
                await queue.PreviousQueueEntry.DiscordMessage.DeleteAsync();
            }
            catch
            {
                return;
            }
        }

        var next = queue.GetNextQueueEntry();
        if (next == null)
        {
            _queueService.SetLastPlayed(player.ChannelId);
            Timeout(player);
            return;
        }
        await player.PlayAsync(next.Track);
        queue.PreviousQueueEntry.DiscordMessage = await next.Channel.SendMessageAsync(EmbedModule.GetTrackPlayingEmbed(next.Track.Info));
    }

    public async void Timeout(LavalinkGuildPlayer player)
    {
        await Task.Delay(timeoutMinutes * 60 * 1000);
        if (_queueService.GetLastPlayed(player.ChannelId) <= DateTime.UtcNow.AddMinutes(-timeoutMinutes))
        {
            if (player.CurrentTrack == null)
            {
                _queueService.RemoveLastPlayed(player.ChannelId);
                _queueService.RemoveQueue(player.ChannelId);
                await player.DisconnectAsync();
            }
        }
    }
}
