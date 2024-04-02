namespace Multi_Bot_Sharp.Modules
{
    public class AudioModule : BaseCommandModule
    {
        private async Task<bool> JoinAsync(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            if (!lavalink.ConnectedSessions.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return false;
            }

            var session = lavalink.ConnectedSessions.Values.First();
            var channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice && channel.Type != ChannelType.Stage)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return false;
            }

            await session.ConnectAsync(channel);
            return true;
        }

        [Command("play")]
        [Description("Plays music")]
        public async Task Play(CommandContext ctx, [RemainingText] string query)
        {
            //var query = string.Join(" ", queryArray);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            if (!JoinAsync(ctx).Result)
            {
                return;
            }

            var lavalink = ctx.Client.GetLavalink();
            var guildPlayer = lavalink.GetGuildPlayer(ctx.Guild);

            if (guildPlayer == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            var loadResult = await guildPlayer.LoadTracksAsync(LavalinkSearchType.Youtube, query);

            // If something went wrong on Lavalink's end or it just couldn't find anything.
            if (loadResult.LoadType == LavalinkLoadResultType.Empty || loadResult.LoadType == LavalinkLoadResultType.Error)
            {
                await ctx.RespondAsync($"Track search failed for {query}.");
                return;
            }

            LavalinkTrack track = loadResult.LoadType switch
            {
                LavalinkLoadResultType.Track => loadResult.GetResultAs<LavalinkTrack>(),
                LavalinkLoadResultType.Playlist => loadResult.GetResultAs<LavalinkPlaylist>().Tracks.First(),
                LavalinkLoadResultType.Search => loadResult.GetResultAs<List<LavalinkTrack>>().First(),
                _ => throw new InvalidOperationException("Unexpected load result type.")
            };

            await guildPlayer.PlayAsync(track);

            await ctx.RespondAsync($"Now playing {track.Info.Title}!");
        }
    }
}
