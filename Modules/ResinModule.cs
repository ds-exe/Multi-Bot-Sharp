namespace Multi_Bot_Sharp.Modules;

public class ResinModule : BaseCommandModule
{
    private readonly Dictionary<string, Game> games = new Dictionary<string, Game>()
    {
        { "hsr", new Game{ MaxResin = 240, ResinsMins = 6 } },
        { "genshin", new Game{ MaxResin = 160, ResinsMins = 8 } },
    };

    [Command("resin")]
    public async Task Resin(CommandContext ctx, [Description("hsr or genshin")] string game, [Description("positive value to set, negative value to reduce")] int? resin = null)
    {
        if (!games.ContainsKey(game))
        {
            await ctx.RespondAsync("Resin error message");
            return;
        }

        if (resin == null)
        {
            await ctx.RespondAsync("Print current games resin embed");
            return;
        }

        await ctx.RespondAsync("TODO");
    }

    [Command("hsr")]
    [Description("Shortcut for resin hsr")]
    public async Task Hsr(CommandContext ctx, [Description("positive value to set, negative value to reduce")] int? resin = null)
    {
        await Resin(ctx, "hsr", resin);
        return;
    }

    [Command("genshin")]
    [Description("Shortcut for resin genshin")]
    public async Task Genshin(CommandContext ctx, [Description("positive value to set, negative value to reduce")] int? resin = null)
    {
        await Resin(ctx, "genshin", resin);
        return;
    }
}
