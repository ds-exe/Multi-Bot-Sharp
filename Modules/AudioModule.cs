﻿namespace Multi_Bot_Sharp.Modules
{
    public class AudioModule : BaseCommandModule
    {
        private const int timeoutMinutes = 15;

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

            var queue = _queueModule.GetQueue(guildPlayer.ChannelId);

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
            if (UtilityModule.IsDM(ctx))
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

            await guildPlayer.DisconnectAsync();
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
            if (guildPlayer == null)
            {
                await ctx.RespondAsync("Nothing is playing.");
                return;
            }

            _queueModule.RemoveQueue(guildPlayer.ChannelId);
            await guildPlayer.StopAsync();
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
                    _queueModule.SetLastPlayed(player.ChannelId);
                    Timeout(player);
                    return;
                }
                await player.PlayAsync(next.Track);
                queue.PreviousQueueEntry.DiscordMessage = await next.Channel.SendMessageAsync(EmbedModule.GetTrackPlayingEmbed(next.Track.Info));
            }
        }

        public async void Timeout(LavalinkGuildPlayer player)
        {
            await Task.Delay(timeoutMinutes * 60 * 1000);
            if (_queueModule.GetLastPlayed(player.ChannelId) <= DateTime.UtcNow.AddMinutes(-timeoutMinutes))
            {
                if (player.CurrentTrack == null)
                {
                    _queueModule.RemoveLastPlayed(player.ChannelId);
                    _queueModule.RemoveQueue(player.ChannelId);
                    await player.DisconnectAsync();
                }
            }
        }
    }
}
