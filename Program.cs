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
            if (config == null || config.Token == null || config.LavalinkPassword == null || config.Prefix == null)
            {
                return;
            };

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.None
            });

            var lavalink = discord.UseLavalink();

            var services = new ServiceCollection()
                .AddSingleton<QueueService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton(discord)
                .BuildServiceProvider();

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string> { config.Prefix },
                ServiceProvider = services
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            commands.SetHelpFormatter<CustomFormatHelper>();

            await discord.ConnectAsync(new DiscordActivity($"{config.Prefix}help", ActivityType.ListeningTo));
            await Task.Delay(15 * 1000);

            await ConfigHelper.ConnectLavalink(lavalink);
            await Task.Delay(-1);
        }
    }
}
