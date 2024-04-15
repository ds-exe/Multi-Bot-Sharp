namespace Multi_Bot_Sharp.Modules;

[Group("resin"), Aliases("hsr")]
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
            await ReduceResinData(ctx, game, Math.Abs((int)resin));
            return;
        }

        if (resin > games[game].MaxResin)
        {
            await ctx.RespondAsync("Resin value too high");
            return;
        }

        var fullTime = DateTime.UtcNow.AddMinutes((games[game].MaxResin - (int)resin) * games[game].ResinsMins);
        SetResinData(ctx, game, fullTime);
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
    }

    private async Task ReduceResinData(CommandContext ctx, string game, int resin)
    {
        var resinData = _databaseService.GetResinData(ctx.Member.Id, game);
        if (resinData == null)
        {
            await ctx.RespondAsync($"No game data found");
        }
        else
        {
            if (GetCurrentResin(resinData) - resin < 0)
            {
                await ctx.RespondAsync($"Not enough resin to reduce");
                return;
            }
            SetResinData(ctx, game, resinData.MaxResinTimestamp.AddMinutes(resin * games[game].ResinsMins));
        }
    }

    private void SetResinData(CommandContext ctx, string game, DateTime fullTime)
    {
        var resinData = new ResinData { UserId = ctx.Member.Id, Game = game, MaxResinTimestamp = fullTime };
        _databaseService.InsertResinData(resinData);

        ctx.RespondAsync(EmbedHelper.GetResinEmbed(resinData, GetCurrentResin(resinData)));

        SetResinNotifications(ctx, resinData);
    }

    private void SetResinNotifications(CommandContext ctx, ResinData resinData)
    {
        _databaseService.ClearOldResinNotifications(ctx.Member.Id, resinData.Game);
        var currentResin = GetCurrentResin(resinData);
        if (currentResin >= games[resinData.Game].MaxResin)
        {
            return;
        }
        SetResinNotification(ctx, resinData, games[resinData.Game].MaxResin);
        if (currentResin >= games[resinData.Game].MaxResin - 20)
        {
            return;
        }
        SetResinNotification(ctx, resinData, games[resinData.Game].MaxResin - 20);
        if (currentResin >= 120)
        {
            return;
        }
        SetResinNotification(ctx, resinData, 120);
    }

    private void SetResinNotification(CommandContext ctx, ResinData resinData, int notificationResin)
    {
        var timeAdjustment = (games[resinData.Game].MaxResin - notificationResin) * games[resinData.Game].ResinsMins;
        var notificationTimestamp = resinData.MaxResinTimestamp.AddMinutes(-timeAdjustment);
        var maxResinNotification = new ResinNotification { UserId = ctx.Member.Id, Game = resinData.Game, NotificationResin = notificationResin, 
            NotificationTimestamp =  notificationTimestamp, MaxResinTimestamp = resinData.MaxResinTimestamp };

        _databaseService.InsertResinNotification(maxResinNotification);
    }

    private int GetCurrentResin(ResinData resinData)
    {
        return (int)Math.Min(Math.Floor(games[resinData.Game].MaxResin -
            (resinData.MaxResinTimestamp - DateTime.UtcNow).TotalMinutes /
            games[resinData.Game].ResinsMins),
            games[resinData.Game].MaxResin);
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

    //[Command("notify")]
    //[Description("Set a custom notification time")]
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
