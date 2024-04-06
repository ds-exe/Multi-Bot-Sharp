namespace Multi_Bot_Sharp.Modules;

[Group("perms")]
[Description("Commands to enable permission for bot usage")]
public class PermissionsModule : BaseCommandModule
{
    [GroupCommand, Command("help")]
    [Description("Lists permission commands")]
    public async Task Help(CommandContext ctx)
    {
        await ctx.RespondAsync(UtilityModule.GetCustomHelpCommand(ctx));
    }

    [Command("listusers")]
    public async Task ListUsers(CommandContext ctx)
    {
        await ctx.RespondAsync("List Users");
    }

    [Command("listroles")]
    public async Task ListRoles(CommandContext ctx)
    {
        await ctx.RespondAsync("List Roles");
    }
}
