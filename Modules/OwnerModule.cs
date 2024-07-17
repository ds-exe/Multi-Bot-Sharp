namespace Multi_Bot_Sharp.Modules;

[Hidden]
public class OwnerModule : BaseCommandModule
{
    private const ulong creatorID = 74968333413257216;
    private ulong owner { get; set; }

    public OwnerModule()
    {
        var config = ConfigHelper.GetJsonObject<Config>("config");
        owner = config.Owner == 0 ? creatorID : config.Owner;
    }

    [Command("restart")]
    public async Task Restart(CommandContext ctx)
    {
        if (ctx.User.Id == owner)
        {
            Environment.Exit(0);
        }
    }
}
