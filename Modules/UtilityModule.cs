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
        var helpCommand = ctx.CommandsNext.FindCommand("help", out var _);
        var commandContext = ctx.CommandsNext.CreateContext(ctx.Message, UtilityModule.GetPrefix(), helpCommand, "perms");
        var customHelpMessage = new CustomHelpFormatter(commandContext).WithCommand(ctx.Command);
        if (ctx.Command is CommandGroup)
        {
            customHelpMessage.WithSubcommands(((CommandGroup)ctx.Command).Children);
        }
        return customHelpMessage.Build().Embed;
    }
}
