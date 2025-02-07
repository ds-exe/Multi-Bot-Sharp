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
            if (config.Token == null || config.LavalinkPassword == null || config.Prefix == null)
            {
                return;
            };

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.None,
                ReconnectIndefinitely = config.ReconnectIndefinitely
            });

            var lavalink = discord.UseLavalink();

            var services = new ServiceCollection()
                .AddSingleton<QueueService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton(discord)
                .BuildServiceProvider();

            // TODO: Remove remaining CommandsNext module
            //-------------------------------------------------------------------------
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string> { config.Prefix },
                ServiceProvider = services,
                EnableDefaultHelp = false
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            //-------------------------------------------------------------------------

            var appCommands = discord.UseApplicationCommands(new ApplicationCommandsConfiguration()
            {
                ServiceProvider = services,
                EnableDefaultHelp = false,
            });

            if (config.TestServer != null)
            {
                foreach (var server in config.TestServer)
                {
                    //await appCommands.CleanGuildCommandsAsync(); // Used to wipe commands after testing
                    appCommands.RegisterGuildCommands(Assembly.GetExecutingAssembly(), server);
                }
            }
            else
            {
                appCommands.RegisterGlobalCommands(Assembly.GetExecutingAssembly());
            }

            discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content == $"{config.Prefix}restart")
                {
                    await OwnerCommandModule.Restart(e.Message);
                }
                else
                {
                    if (e.Message.Content.StartsWith(config.Prefix))
                    {
                        await e.Message.RespondAsync("Please use slash commands.");
                    }
                }
            };

            await discord.ConnectAsync(new DiscordActivity($"/help", ActivityType.ListeningTo));
            await Task.Delay(15 * 1000);

            await ConfigHelper.ConnectLavalink(lavalink);
            await Task.Delay(-1);
        }
    }
}
