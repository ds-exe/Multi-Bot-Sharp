using System.Reflection;

namespace Multi_Bot_Sharp
{
    internal class Program
    {
        private static AudioModule _audioModule;

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            string cwd = Directory.GetCurrentDirectory();
            string path = cwd + @"/config.json";

            #if DEBUG
            path = cwd + @"/../../../config.json";
            #endif

            string text = File.ReadAllText(path);

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

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string> { config.Prefix }
            });
            //commands.RegisterCommands(Assembly.GetExecutingAssembly());
            commands.RegisterCommands<AudioModule>();

            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            //var services = ConfigureServices(discord);
            //InitialiseModules(services);

            //discord.MessageCreated += async (s, e) =>
            //{

            //    if (!e.Message.Author.IsBot && e.Message.Content.ToLower().StartsWith(config.Token))
            //    {
            //        var data = e.Message.Content.Substring(config.Token.Length);
            //        var parameterList = data.Split(" ");

            //        await HandleCommand(e.Message, parameterList.First(), parameterList.Skip(1).ToList());
            //    }
            //};

            await Task.Delay(-1);
        }

        //static async Task HandleCommand(DiscordMessage message, string command, List<string> parameters)
        //{
        //    switch (command)
        //    {
        //        case "play":
        //            _audioModule.Play(message, parameters);
        //            break;
        //        default:
        //            await message.RespondAsync("Unknown command");
        //            break;
        //    }
        //}

        //static void InitialiseModules(ServiceProvider services)
        //{
        //    _audioModule = services.GetRequiredService<AudioModule>();
        //}

        //static ServiceProvider ConfigureServices(DiscordClient discord)
        //{
        //    return new ServiceCollection()
        //        .AddSingleton(discord)
        //        .AddSingleton<AudioModule>()
        //        .BuildServiceProvider();
        //}
    }
}
