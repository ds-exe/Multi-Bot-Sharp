namespace Multi_Bot_Sharp.Modules;

[Group("resin")]
[Description("Resin commands")]
public class ResinModule : BaseCommandModule
{
    private readonly Dictionary<string, Game> games = new Dictionary<string, Game>()
    {
        { "hsr", new Game{ MaxResin = 240, ResinsMins = 6 } },
        { "genshin", new Game{ MaxResin = 160, ResinsMins = 8 } },
    };

    DatabaseService _databaseService;

    public ResinModule(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task Resin(CommandContext ctx, [Description("hsr or genshin")] string game, [Description("positive value to set, negative value to reduce")] int? resin = null)
    {
        if (!games.ContainsKey(game))
        {
            await ctx.RespondAsync("Resin error message");
            return;
        }

        if (resin == null)
        {
            await ctx.RespondAsync($"Print {game} resin embed");
            return;
        }

        await _databaseService.GetResinData();
        await ctx.RespondAsync("TODO");
    }

    [GroupCommand, Command("hsr")]
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

    [Command("notify")]
    [Description("Set a custom notification time")]
    public async Task Notify(CommandContext ctx, int resin = 0)
    {
        if (resin < 30)
        {
            await ctx.RespondAsync("Cannot notify at less than 30");
            return;
        }
        await ctx.RespondAsync("Not implemented");
        return;
    }
}
