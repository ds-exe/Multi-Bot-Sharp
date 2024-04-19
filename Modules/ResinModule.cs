﻿namespace Multi_Bot_Sharp.Modules;

[Group("resin"), Aliases("hsr")]
[Description("Resin commands")]
public class ResinModule : BaseCommandModule
{
    private static readonly Dictionary<string, GameResin> games = new Dictionary<string, GameResin>()
    {
        { "hsr", new GameResin{ MaxResin = 240, ResinsMins = 6 } },
        { "genshin", new GameResin{ MaxResin = 160, ResinsMins = 8 } },
    };

    private DatabaseService _databaseService;
    private static System.Timers.Timer _timer;
    private static DiscordClient _discordClient;

    public ResinModule(DiscordClient client, DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _discordClient = client;

        client.ComponentInteractionCreated += async (s, e) =>
        {
            await HandleButtons(e);
        };
        StartNotificationTimer();
    }

    public async Task Resin(CommandContext ctx, [Description("hsr or genshin")] string game, [Description("positive value to set, negative value to reduce")] int? resin = null)
    {
        if (!games.ContainsKey(game))
        {
            await ctx.RespondAsync("Entered game is not supported.");
            return;
        }

        if (resin == null)
        {
            await SendResinData(ctx.Message, ctx.Message.Author.Id, game);
            return;
        }

        if (resin < 0)
        {
            await ReduceResinData(ctx.Message, ctx.Message.Author.Id, game, Math.Abs((int)resin));
            return;
        }

        if (resin > games[game].MaxResin)
        {
            await ctx.RespondAsync("Resin value too high");
            return;
        }

        var fullTime = DateTime.UtcNow.AddMinutes((games[game].MaxResin - (int)resin) * games[game].ResinsMins);
        await SetResinData(ctx.Message, ctx.Message.Author.Id, game, fullTime);
    }

    private async Task SendResinData(DiscordMessage message, ulong userId, string game)
    {
        var resinData = _databaseService.GetResinData(userId, game);
        if (resinData == null)
        {
            await message.RespondAsync($"No game data found");
        }
        else
        {
            await message.RespondAsync(GetResinDataMessage(resinData));
        }
    }

    private DiscordMessageBuilder GetResinDataMessage(ResinData resinData)
    {
        var nextNotification = _databaseService.GetNextResinNotification(resinData);
        var customResin = _databaseService.GetCustomResinData(resinData.UserId, resinData.Game);

        return new DiscordMessageBuilder()
            .AddEmbed(EmbedHelper.GetResinEmbed(resinData, nextNotification, GetCurrentResin(resinData)))
            .AddComponents(EmbedHelper.GetButtons(customResin != null ? customResin.Resin : 120))
            .AddComponents(EmbedHelper.GetButtons2(customResin != null ? customResin.Resin : 120));
    }

    private async Task ReduceResinData(DiscordMessage message, ulong userId, string game, int resin)
    {
        var resinData = _databaseService.GetResinData(userId, game);
        if (resinData == null)
        {
            await message.RespondAsync($"No game data found");
        }
        else
        {
            if (GetCurrentResin(resinData) - resin < 0)
            {
                await message.RespondAsync($"Not enough resin to reduce");
                return;
            }
            await SetResinData(message, userId, game, resinData.MaxResinTimestamp.AddMinutes(resin * games[game].ResinsMins));
        }
    }

    private async Task SetResinData(DiscordMessage message, ulong userId, string game, DateTime fullTime)
    {
        var resinData = new ResinData { UserId = userId, Game = game, MaxResinTimestamp = fullTime };
        _databaseService.InsertResinData(resinData);

        SetResinNotifications(message, userId, resinData);

        await SendResinData(message, userId, game);
    }

    private void SetResinNotifications(DiscordMessage message, ulong userId, ResinData resinData)
    {
        _databaseService.ClearOldResinNotifications(userId, resinData.Game);
        var currentResin = GetCurrentResin(resinData);
        if (currentResin < games[resinData.Game].MaxResin)
        {
            SetResinNotification(message, userId, resinData, games[resinData.Game].MaxResin);
        }
        if (currentResin < games[resinData.Game].MaxResin - 20)
        {
            SetResinNotification(message, userId, resinData, games[resinData.Game].MaxResin - 20);
        }
        var customResin = _databaseService.GetCustomResinData(userId, resinData.Game);
        if (customResin != null && currentResin < customResin.Resin)
        {
            SetResinNotification(message, userId, resinData, customResin.Resin);
        }
    }

    private void SetResinNotification(DiscordMessage message, ulong userId, ResinData resinData, int notificationResin)
    {
        var timeAdjustment = (games[resinData.Game].MaxResin - notificationResin) * games[resinData.Game].ResinsMins;
        var notificationTimestamp = resinData.MaxResinTimestamp.AddMinutes(-timeAdjustment);
        var maxResinNotification = new ResinNotification { UserId = userId, Game = resinData.Game,
            NotificationTimestamp = notificationTimestamp, MaxResinTimestamp = resinData.MaxResinTimestamp };

        _databaseService.InsertResinNotification(maxResinNotification);
    }

    private static int GetCurrentResin(ResinData resinData)
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

    [Command("notify")]
    [Description("Set a custom notification value")]
    public async Task Notify(CommandContext ctx, int? resin = null, string game = "hsr")
    {
        if (!games.ContainsKey(game))
        {
            await ctx.RespondAsync("Entered game is not supported.");
            return;
        }
        if (resin == null)
        {
            var customResinData = _databaseService.GetCustomResinData(ctx.Message.Author.Id, game);
            if (customResinData != null)
            {
                await ctx.RespondAsync($"Current resin notification: {customResinData.Resin}");
            }
            else
            {
                await ctx.RespondAsync("No custom resin value set");
            }
            return;
        }
        if (resin < 30)
        {
            await ctx.RespondAsync("Cannot notify at less than 30");
            return;
        }
        if (resin > games[game].MaxResin)
        {
            await ctx.RespondAsync("Cannot notify at less than 30");
            return;
        }
        _databaseService.InsertCustomResinData(new CustomResinData { UserId = ctx.Message.Author.Id, Game = game, Resin = (int)resin });
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
        return;
    }

    public void StartNotificationTimer()
    {
        _timer = new System.Timers.Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += SendNotifications;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    public async void SendNotifications(Object? source, ElapsedEventArgs e)
    {
        var timeNow = DateTime.UtcNow;
        var resinNotifications = _databaseService.GetElapsedResinNotifications(timeNow);
        _databaseService.DeleteElapsedResinNotifications(timeNow);

        foreach (var resinNotification in resinNotifications)
        {
            try
            {
                var user = await _discordClient.GetUserAsync(resinNotification.UserId);
                await user.SendMessageAsync(GetResinDataMessage(resinNotification));
            }
            catch { }
        }
    }

    async Task HandleButtons(ComponentInteractionCreateEventArgs e)
    {
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var game = e.Message.Embeds.First().Title;
        game = game.Replace("Honkai: Star Rail", "hsr");
        game = game.Replace("Genshin", "genshin");
        game = Regex.Replace(game, " |:/ g", "");

        var customResin = _databaseService.GetCustomResinData(e.User.Id, game);
        switch (e.Id)
        {
            case "lowResin":
                await ReduceResinData(e.Message, e.User.Id, game, 10);
                break;
            case "midResin":
                await ReduceResinData(e.Message, e.User.Id, game, 30);
                break;
            case "highResin":
                await ReduceResinData(e.Message, e.User.Id, game, 40);
                break;
            case "customResin":
                if (customResin != null)
                {
                    await ReduceResinData(e.Message, e.User.Id, game, customResin.Resin);
                }
                break;
            case "customResin2":
                if (customResin != null)
                {
                    await ReduceResinData(e.Message, e.User.Id, game, customResin.Resin * 2);
                }
                break;
            case "refresh":
                await SendResinData(e.Message, e.User.Id, game);
                break;
        }
    }
}
