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
            var text = ConfigHelper.GetJsonText("config");
            var config = JsonSerializer.Deserialize<Config>(text);
            if (config == null || config.Token == null || config.LavalinkPassword == null || config.Prefix == null)
            {
                return;
            };

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent
            });

            var lavalink = discord.UseLavalink();

            var services = new ServiceCollection()
                .AddSingleton<QueueService>()
                .AddSingleton<DatabaseService>()
                .BuildServiceProvider();

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string> { config.Prefix },
                ServiceProvider = services
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            commands.SetHelpFormatter<CustomFormatHelper>();

            await discord.ConnectAsync();
            await Task.Delay(15 * 1000);

            await ConfigHelper.ConnectLavalink(lavalink);
            await Task.Delay(-1);
        }
    }
}
