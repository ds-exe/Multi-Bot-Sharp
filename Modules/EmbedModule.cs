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
    }
}
