namespace Multi_Bot_Sharp.Modules
{
    public class EmbedModule
    {
        public static DiscordEmbed GetTimestampEmbed(string time)
        {
            var embed = new DiscordEmbedBuilder();
            embed.Color = new DiscordColor("00FFFF");
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
    }
}
