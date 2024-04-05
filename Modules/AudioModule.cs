namespace Multi_Bot_Sharp.Modules
{
    public class AudioModule : BaseCommandModule
    {
        private QueueModule _queueModule;

        public AudioModule(QueueModule queueModule)
        {
            _queueModule = queueModule;
        }

        private async Task<bool> JoinAsync(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            if (!lavalink.ConnectedSessions.Any())
            {
                await ctx.RespondAsync("Music connection error.");
                return false;
            }

            var session = lavalink.ConnectedSessions.Values.First();
            var channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice && channel.Type != ChannelType.Stage)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return false;
            }

            var player = await session.ConnectAsync(channel);

            player.TrackStarted += Player_TrackStarted;
            player.TrackEnded += Player_TrackEnded;
            return true;
        }

        [Command("play")]
        [Description("Plays music")]
        public async Task Play(CommandContext ctx, [RemainingText] string query)
        {
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
                    await ctx.RespondAsync("Music connection error.");
                    return;
                }
                guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
            }

            var loadResult = await guildPlayer.LoadTracksAsync(LavalinkSearchType.Youtube, query);

            // If something went wrong on Lavalink's end or it just couldn't find anything.
            if (loadResult.LoadType == LavalinkLoadResultType.Empty || loadResult.LoadType == LavalinkLoadResultType.Error)
            {
                await ctx.RespondAsync($"Track search failed for {query}.");
                return;
            }

            var queue = _queueModule.GetQueue(guildPlayer.ChannelId);
            if (queue == null)
            {
                queue = _queueModule.AddQueue(guildPlayer.ChannelId);
            }

            if (loadResult.LoadType == LavalinkLoadResultType.Track)
            {
                QueueTrack(ctx, queue, loadResult.GetResultAs<LavalinkTrack>());
            }
            else if(loadResult.LoadType == LavalinkLoadResultType.Playlist)
            {
                QueuePlaylist(ctx, queue, loadResult.GetResultAs<LavalinkPlaylist>());
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

        private void QueueTrack(CommandContext ctx, Queue queue, LavalinkTrack track)
        {
            ctx.RespondAsync($"Track {track.Info.Title} added");
            queue.AddTrack(ctx.Channel, track);
        }

        private void QueuePlaylist(CommandContext ctx, Queue queue, LavalinkPlaylist playlist)
        {
            ctx.RespondAsync($"Playlist {playlist.Info.Name} added");
            foreach (var track in playlist.Tracks)
            {
                queue.AddTrack(ctx.Channel, track);
            }
        }

        [Command("skip")]
        [Description("Skips track")]
        public async Task Skip(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
            if (guildPlayer == null)
            {
                await ctx.RespondAsync("Nothing is playing.");
                return;
            }

            await guildPlayer.StopAsync();
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
        }

        [Command("leave")]
        [Description("Leaves channel")]
        public async Task Leave(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
            if (guildPlayer == null)
            {
                await ctx.RespondAsync("Nothing is playing.");
                return;
            }

            await guildPlayer.DisconnectAsync();
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
        }

        [Command("stop")]
        [Description("Stops playback")]
        public async Task Stop(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);
            if (guildPlayer == null)
            {
                await ctx.RespondAsync("Nothing is playing.");
                return;
            }

            _queueModule.RemoveQueue(guildPlayer.ChannelId);
            await guildPlayer.StopAsync();
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
        }

        private async Task Player_TrackStarted(LavalinkGuildPlayer sender, DisCatSharp.Lavalink.EventArgs.LavalinkTrackStartedEventArgs e)
        {
            var track = e.Track;
            var info = e.Track.Info;
            return;
        }

        private async Task Player_TrackEnded(LavalinkGuildPlayer sender, DisCatSharp.Lavalink.EventArgs.LavalinkTrackEndedEventArgs e)
        {
            PlayQueueAsync(sender, _queueModule.GetQueue(sender.ChannelId));
        }

        public async void PlayQueueAsync(LavalinkGuildPlayer player, Queue queue)
        {
            if (queue == null)
            {
                return;
            }

            if (player.CurrentTrack == null)
            {
                if (queue.PreviousQueueEntry != null)
                {
                    await queue.PreviousQueueEntry.DiscordMessage.DeleteAsync();
                }

                var next = queue.GetNextQueueEntry();
                if (next == null)
                {
                    return;
                }
                await player.PlayAsync(next.Track);
                queue.PreviousQueueEntry.DiscordMessage = await next.Channel.SendMessageAsync($"Track is playing");
            }
        }
    }
}
