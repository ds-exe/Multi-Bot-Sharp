namespace Multi_Bot_Sharp.Modules;

[Group("resin")]
[Description("Resin commands")]
public class ResinModule : BaseCommandModule
{
    private readonly Dictionary<string, GameResin> games = new Dictionary<string, GameResin>()
    {
        { "hsr", new GameResin{ MaxResin = 240, ResinsMins = 6 } },
        { "genshin", new GameResin{ MaxResin = 160, ResinsMins = 8 } },
    };

    private DatabaseService _databaseService;

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
            await SendResinData(ctx, game);
            return;
        }

        if (resin < 0)
        {
            //TODO Reduce resin function
            await ctx.RespondAsync("TODO Reduce resin function");
            return;
        }

        SetResinData(ctx, game, (int)resin);
    }

    private async Task SendResinData(CommandContext ctx, string game)
    {
        var resinData = _databaseService.GetResinData(ctx.Member.Id, game);
        if (resinData == null)
        {
            await ctx.RespondAsync($"No game data found");
        }
        else
        {
            await ctx.RespondAsync(EmbedHelper.GetResinEmbed(resinData, GetCurrentResin(resinData)));
        }
        return;
    }

    private void SetResinData(CommandContext ctx, string game, int resin)
    {
        var fullTime = DateTime.UtcNow.AddMinutes((games[game].MaxResin - resin) * games[game].ResinsMins);
        _databaseService.InsertResinData(new ResinData { UserId = ctx.Member.Id, Game = game, MaxResinTimestamp = fullTime });
    }

    private int GetCurrentResin(ResinData resinData)
    {
        return (int)Math.Floor(games[resinData.Game].MaxResin -
            (resinData.MaxResinTimestamp - DateTime.UtcNow).TotalMinutes /
            games[resinData.Game].ResinsMins);
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
