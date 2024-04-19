namespace Multi_Bot_Sharp.Modules;

public class AvatarModule : BaseCommandModule
{
    [Command("avatar")]
    public async Task Avatar(CommandContext ctx, DiscordUser? user = null)
    {
        if (user == null)
        {
            await ctx.RespondAsync(ctx.Message.Author.AvatarUrl);
            return;
        }
        await ctx.RespondAsync(user.AvatarUrl);
    }
}
