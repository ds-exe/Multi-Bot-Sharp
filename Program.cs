﻿namespace Multi_Bot_Sharp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var text = UtilityModule.GetJsonText("config");
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

            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = config.LavalinkPassword, // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            var lavalink = discord.UseLavalink();

            var services = new ServiceCollection()
                //.AddSingleton<EmbedModule>()
                //.AddSingleton<UtilityModule>()
                .BuildServiceProvider();

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string> { config.Prefix },
                ServiceProvider = services
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            commands.SetHelpFormatter<CustomHelpFormatter>();

            await discord.ConnectAsync();
            try
            {
                await lavalink.ConnectAsync(lavalinkConfig);
            }
            catch
            {
                Console.WriteLine("Lavalink connection error.");
            }

            await Task.Delay(-1);
        }
    }
}
