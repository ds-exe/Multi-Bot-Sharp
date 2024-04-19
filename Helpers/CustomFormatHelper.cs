﻿namespace Multi_Bot_Sharp.Helpers;

public class CustomFormatHelper : DefaultHelpFormatter
{
    private const ulong creatorID = 74968333413257216;
    private DiscordUser User;
    private string EmbedThumbnail;

    public CustomFormatHelper(CommandContext ctx) : base(ctx)
    {
        var config = JsonSerializer.Deserialize<Config>(ConfigHelper.GetJsonText("config"));
        User = ctx.Client.GetUserAsync(config?.Owner ?? creatorID).Result;
        EmbedThumbnail = config?.EmbedThumbnail ?? "";
    }

    public override CommandHelpMessage Build()
    {
        EmbedBuilder.Color = new DiscordColor("0099ff");
        EmbedBuilder.WithThumbnail(EmbedThumbnail);
        EmbedBuilder.WithFooter($"BOT owner @{User.Username}", User.AvatarUrl);
        return base.Build();
    }
}
