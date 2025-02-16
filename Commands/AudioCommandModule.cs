namespace Multi_Bot_Sharp.Commands;

public class AudioCommandModule : ApplicationCommandsModule
{
    public required QueueService _queueService;

    [SlashCommand("play", "Plays the given link / query.")]
    public async Task PlayCommand(InteractionContext ctx, [Option("query", "Selected Query")] string query, [Option("shuffle", "Enable shuffle")] bool shuffle = false)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Cannot play in DM's.",
                IsEphemeral = true,
            });
            return;
        }

        if (string.IsNullOrEmpty(query))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "No query given.",
                IsEphemeral = true,
            });
            return;
        }

        if (ctx.Member?.VoiceState == null || ctx.Member.VoiceState.Channel?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "You are not in a voice channel.",
                IsEphemeral = true,
            });
            return;
        }

        var perms = ctx.Member.VoiceState?.Channel?.PermissionsFor(await ctx.Client.CurrentUser.ConvertToMember(ctx.Guild)) ?? Permissions.None;
        var hasPerms = perms.HasPermission(Permissions.UseVoice) && perms.HasPermission(Permissions.Speak) && perms.HasPermission(Permissions.AccessChannels);
        if (!hasPerms)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Missing required permissions.",
                IsEphemeral = true,
            });
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
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = $"Track search failed for {query}.",
                IsEphemeral = true,
            });
            return;
        }

        var queue = _queueService.AddQueue(guildPlayer.GuildId, guildPlayer);

        if (loadResult.LoadType == LavalinkLoadResultType.Track)
        {
            await QueueTrack(ctx, queue, loadResult.GetResultAs<LavalinkTrack>());
        }
        else if (loadResult.LoadType == LavalinkLoadResultType.Playlist)
        {
            await QueuePlaylist(ctx, queue, loadResult.GetResultAs<LavalinkPlaylist>(), query);
        }
        else if (loadResult.LoadType == LavalinkLoadResultType.Search)
        {
            await QueueTrack(ctx, queue, loadResult.GetResultAs<List<LavalinkTrack>>().First());
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

    [SlashCommand("shuffle", "Shuffles the queue.")]
    public async Task ShuffleCommand(InteractionContext ctx)
    {
        await Shuffle(ctx);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            Content = "Queue shuffled.",
        });
    }

    [SlashCommand("skip", "Skips the current track.")]
    public async Task Skip(InteractionContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Cannot play in DM's.",
                IsEphemeral = true,
            });
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null || guildPlayer.CurrentTrack == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Nothing is playing.",
                IsEphemeral = true,
            });
            return;
        }

        await guildPlayer.StopAsync();
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            Content = "Track Skipped.",
        });
    }

    [SlashCommand("stop", "Stops playback.")]
    public async Task Stop(InteractionContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Cannot play in DM's.",
                IsEphemeral = true,
            });
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null || guildPlayer.CurrentTrack == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Nothing is playing.",
                IsEphemeral = true,
            });
            return;
        }

        _queueService.ClearQueue(guildPlayer.GuildId);
        await guildPlayer.StopAsync();
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            Content = "Playback stopped.",
        });
    }

    [SlashCommand("leave", "Leaves the call.")]
    public async Task Leave(InteractionContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Cannot play in DM's.",
                IsEphemeral = true,
            });
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Not in voice.",
                IsEphemeral = true,
            });
            return;
        }

        _queueService.ClearQueue(guildPlayer.GuildId);
        _queueService.RemoveQueue(guildPlayer.GuildId);
        await guildPlayer.DisconnectAsync();
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            Content = "Disconnected.",
        });
    }

    [SlashCommand("nowplaying", "Displays current track.")]
    public async Task NowPlaying(InteractionContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Cannot play in DM's.",
                IsEphemeral = true,
            });
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null || guildPlayer.CurrentTrack == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Nothing is playing.",
                IsEphemeral = true,
            });
            return;
        }
        var user = _queueService.GetQueue(guildPlayer.GuildId)?.GetCurrentTrackUser();
        if (user == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Nothing is playing.",
                IsEphemeral = true,
            });
            return;
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        {
            IsEphemeral = true,
        }
        .AddEmbed(EmbedHelper.GetNowPlayingEmbed(guildPlayer.CurrentTrack.Info, user)));
    }

    private async Task<bool> JoinAsync(InteractionContext ctx)
    {
        var lavalink = ctx.Client.GetLavalink();
        if (!lavalink.ConnectedSessions.Any())
        {
            await ctx.Channel.SendMessageAsync("Music connection error, attempting to reconnect player.");
            await ConfigHelper.ConnectLavalink(lavalink);

            if (!lavalink.ConnectedSessions.Any())
            {
                await ctx.Channel.SendMessageAsync("Music connection failed to restart.");
                return false;
            }
        }

        var session = lavalink.ConnectedSessions.Values.First();
        var channel = ctx.Member?.VoiceState?.Channel;

        if (channel?.Type != ChannelType.Voice && channel?.Type != ChannelType.Stage)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Not a valid voice channel.",
                IsEphemeral = true,
            });
            return false;
        }

        await session.ConnectAsync(channel);

        return true;
    }

    protected async Task Shuffle(InteractionContext ctx)
    {
        if (ctx.Channel.IsPrivate || ctx.Guild?.Id == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Cannot play in DM's.",
                IsEphemeral = true,
            });
            return;
        }
        var lavalink = ctx.Client.GetLavalink();
        var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
        if (guildPlayer == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Nothing is playing.",
                IsEphemeral = true,
            });
            return;
        }

        var queue = _queueService.GetQueue(guildPlayer.GuildId);

        queue?.Shuffle();
    }

    protected async Task QueueTrack(InteractionContext ctx, Queue queue, LavalinkTrack track)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(EmbedHelper.GetTrackAddedEmbed(track.Info, ctx.User)));
        queue.AddTrack(ctx.Channel, ctx.User, track);
    }

    protected async Task QueuePlaylist(InteractionContext ctx, Queue queue, LavalinkPlaylist playlist, string url)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(EmbedHelper.GetPlaylistAddedEmbed(playlist, ctx.User, url)));
        foreach (var track in playlist.Tracks)
        {
            queue.AddTrack(ctx.Channel, ctx.User, track);
        }
    }
}
