namespace Multi_Bot_Sharp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var config = ConfigHelper.GetJsonObject<Config>("config");
            if (config.Token == null || config.LavalinkPassword == null)
            {
                return;
            };

            var loggingLevel = Microsoft.Extensions.Logging.LogLevel.None;
            if (config.Debug)
            {
                loggingLevel = Microsoft.Extensions.Logging.LogLevel.Information;
            }

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent,
                MinimumLogLevel = loggingLevel,
                ReconnectIndefinitely = config.ReconnectIndefinitely
            });

            var lavalink = discord.UseLavalink();

            var services = new ServiceCollection()
                .AddSingleton<QueueService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton(discord)
                .BuildServiceProvider();

            var appCommands = discord.UseApplicationCommands(new ApplicationCommandsConfiguration()
            {
                ServiceProvider = services,
                EnableDefaultHelp = false,
            });

            if (config.TestServer != null)
            {
                foreach (var server in config.TestServer)
                {
                    appCommands.RegisterGuildCommands(Assembly.GetExecutingAssembly(), server);
                }
            }
            else
            {
                appCommands.RegisterGlobalCommands(Assembly.GetExecutingAssembly());
            }

            discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content == $"!restart")
                {
                    await OwnerCommandModule.Restart(e.Message);
                }
                else if (e.Message.Content.StartsWith("!"))
                {
                    await e.Message.RespondAsync("Please use slash commands.");
                }
            };

            await discord.ConnectAsync(new DiscordActivity($"/help", ActivityType.ListeningTo));
            await Task.Delay(15 * 1000);

            await ConfigHelper.ConnectLavalink(lavalink);
            await Task.Delay(-1);
        }
    }
}
