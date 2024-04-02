namespace Multi_Bot_Sharp.Modules
{
    public class CustomHelpFormatter : DefaultHelpFormatter
    {
        public EmbedModule EmbedModule { private get; set; }

        private const ulong creatorID = 74968333413257216;

        private DiscordUser User;

        private string EmbedThumbnail;

        public CustomHelpFormatter(CommandContext ctx) : base(ctx)
        {
            _ = InitialiseEmbeds(ctx.Client);
        }

        public override CommandHelpMessage Build()
        {
            EmbedBuilder.Color = new DiscordColor("0099ff");
            EmbedBuilder.WithThumbnail(EmbedThumbnail);
            EmbedBuilder.WithFooter($"BOT made by @{User.Username}", User.AvatarUrl);
            return base.Build();
        }

        private async Task InitialiseEmbeds(DiscordClient client)
        {
            User = await client.GetUserAsync(creatorID);

            string cwd = Directory.GetCurrentDirectory();
            string path = cwd + @"/config.json";

            #if DEBUG
            path = cwd + @"/../../../config.json";
            #endif

            string text = File.ReadAllText(path);

            EmbedThumbnail = JsonSerializer.Deserialize<Config>(text).EmbedThumbnail;
        }
    }
}
