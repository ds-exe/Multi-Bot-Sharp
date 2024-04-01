using System.Runtime.InteropServices;

namespace Multi_Bot_Sharp.Modules
{
    internal class AudioModule : BaseCommandModule
    {
        [Command("test")]
        public async Task Test(CommandContext ctx)
        {
            await ctx.RespondAsync("Greetings! UwU");
        }

        [Command("join")]
        public async Task JoinAsync(CommandContext ctx)
        {
            //await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var lavalink = ctx.Client.GetLavalink();
            if (!lavalink.ConnectedSessions.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            var session = lavalink.ConnectedSessions.Values.First();
            var channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice && channel.Type != ChannelType.Stage)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            await session.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined {channel.Mention}!");
        }

        [Command("play")]
        public async Task Play(CommandContext ctx, string query)
        {
            Console.WriteLine(query);
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
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
