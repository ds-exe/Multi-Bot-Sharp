namespace Multi_Bot_Sharp.Modules;

public class UtilityModule
{
    public static string GetJsonText(string file)
    {
        string cwd = Directory.GetCurrentDirectory();
        string path = cwd + $"/{file}.json";

        #if DEBUG
        path = cwd + $"/../../../{file}.json";
        #endif

        return File.ReadAllText(path);
    }

    public static string GetPrefix()
    {
        return JsonSerializer.Deserialize<Config>(GetJsonText("config")).Prefix;
    }

    public static bool IsDM(CommandContext ctx)
    {
        return ctx.Channel.IsPrivate;
    }

    public static DiscordEmbed GetCustomHelpCommand(CommandContext ctx)
    {
        var command = ctx.Command;
        if (command.Name.ToLower() == "help")
        {
            command = command.Parent;
        }
        var helpCommand = ctx.CommandsNext.FindCommand("help", out var _);
        var commandContext = ctx.CommandsNext.CreateContext(ctx.Message, UtilityModule.GetPrefix(), helpCommand, "perms");
        var customHelpMessage = new CustomHelpFormatter(commandContext).WithCommand(command);
        if (command is CommandGroup)
        {
            customHelpMessage.WithSubcommands(((CommandGroup)command).Children);
        }
        return customHelpMessage.Build().Embed;
    }

    public async static Task ConnectLavalink(LavalinkExtension lavalink)
    {
        var config = JsonSerializer.Deserialize<Config>(GetJsonText("config"));
        if (config?.LavalinkPassword == null)
        {
            return;
        };

        var endpoint = new ConnectionEndpoint
        {
            Hostname = "lavalink", // From your server configuration.
            Port = 2333 // From your server configuration
        };

        #if DEBUG
        endpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1", // From your server configuration.
            Port = 2333 // From your server configuration
        };
        #endif

        var lavalinkConfig = new LavalinkConfiguration
        {
            Password = config.LavalinkPassword, // From your server configuration.
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint
        };

        try
        {
            await lavalink.ConnectAsync(lavalinkConfig);
        }
        catch
        {
            //Console.WriteLine("Lavalink connection error.");
            //Environment.Exit(1);
        }
    }
}
