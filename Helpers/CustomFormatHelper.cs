namespace Multi_Bot_Sharp.Helpers;

public class CustomFormatHelper : DefaultHelpFormatter
{
    private const ulong creatorID = 74968333413257216;
    private DiscordUser User;
    private string EmbedThumbnail;

    public CustomFormatHelper(CommandContext ctx) : base(ctx)
    {
        InitialiseEmbeds(ctx.Client);
    }

    public override CommandHelpMessage Build()
    {
        EmbedBuilder.Color = new DiscordColor("0099ff");
        EmbedBuilder.WithThumbnail(EmbedThumbnail);
        EmbedBuilder.WithFooter($"BOT made by @{User.Username}", User.AvatarUrl);
        return base.Build();
    }

    private void InitialiseEmbeds(DiscordClient client)
    {
        User = client.GetUserAsync(creatorID).Result;
        var text = ConfigHelper.GetJsonText("config");
        EmbedThumbnail = JsonSerializer.Deserialize<Config>(text).EmbedThumbnail;
    }
}
