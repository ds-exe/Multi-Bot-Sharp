using System.Text;

namespace Multi_Bot_Sharp.Commands;

public class HelpCommandModule : ApplicationCommandsModule
{
    private const ulong CreatorId = 74968333413257216;
    private readonly DiscordUser _user;
    private readonly string _embedThumbnail;

    public HelpCommandModule(DiscordClient discordClient)
    {
        var config = ConfigHelper.GetJsonObject<Config>("config");
        var botOwner = config.Owner == 0 ? CreatorId : config.Owner;
        _user = discordClient.GetUserAsync(botOwner).Result;
        _embedThumbnail = config.EmbedThumbnail ?? "";
    }

    // Initial code based on DisCatSharp default help command
    [SlashCommand("help", "Displays command help")]
    public async Task DefaultHelpAsync(
        InteractionContext ctx,
        [Autocomplete(typeof(DefaultHelpAutoCompleteProvider)), Option("option_one", "top level command to provide help for", true)] string commandName)
    {
        List<DiscordApplicationCommand> applicationCommands;
        var globalCommandsTask = ctx.Client.GetGlobalApplicationCommandsAsync();
        if (ctx.Guild is not null)
        {
            var guildCommandsTask = ctx.Client.GetGuildApplicationCommandsAsync(ctx.Guild.Id);
            await Task.WhenAll(globalCommandsTask, guildCommandsTask).ConfigureAwait(false);
            applicationCommands = globalCommandsTask.Result.Concat(guildCommandsTask.Result)
                .Where(ac => !ac.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                .GroupBy(ac => ac.Name).Select(x => x.First())
                .ToList();
        }
        else
        {
            await Task.WhenAll(globalCommandsTask).ConfigureAwait(false);
            applicationCommands = globalCommandsTask.Result
                .Where(ac => !ac.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                .GroupBy(ac => ac.Name).Select(x => x.First())
                .ToList();
        }

        if (applicationCommands.Count < 1)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("There are no slash commands").AsEphemeral()).ConfigureAwait(false);
            return;
        }

        var command = applicationCommands.FirstOrDefault(cm => cm.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        if (command is null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"No command called {commandName} in guild {ctx.Guild?.Name}").AsEphemeral()).ConfigureAwait(false);
            return;
        }

        var discordEmbed = new DiscordEmbedBuilder
        {
            Title = "Help",
            Description = $"{command.Mention}: {command.Description ?? "No description provided."}"
        }.AddField(new DiscordEmbedField("Command is NSFW", command.IsNsfw.ToString()));
        // Custom help start
        discordEmbed.Color = new DiscordColor("0099ff");
        discordEmbed.WithThumbnail(_embedThumbnail);
        discordEmbed.WithFooter($"BOT owner @{_user.Username}", _user.AvatarUrl);
        // Custom help end
        if (command.Options is not null)
        {
            var commandOptions = command.Options.ToList();
            var sb = new StringBuilder();

            foreach (var option in commandOptions)
                sb.Append('`').Append(option.Name).Append("`: ").Append(option.Description ?? "No description provided.").Append('\n');

            sb.Append('\n');
            discordEmbed.AddField(new DiscordEmbedField("Arguments", sb.ToString().Trim()));
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(discordEmbed).AsEphemeral()).ConfigureAwait(false);
    }

    private sealed class DefaultHelpAutoCompleteProvider : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
        {
            IEnumerable<DiscordApplicationCommand> slashCommands;
            var globalCommandsTask = context.Client.GetGlobalApplicationCommandsAsync();
            if (context.Guild is not null)
            {
                var guildCommandsTask = context.Client.GetGuildApplicationCommandsAsync(context.Guild.Id);
                await Task.WhenAll(globalCommandsTask, guildCommandsTask).ConfigureAwait(false);
                slashCommands = globalCommandsTask.Result.Concat(guildCommandsTask.Result)
                    .Where(ac => !ac.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(ac => ac.Name).Select(x => x.First())
                    .Where(ac => ac.Name.StartsWith(context.Options[0].Value.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                await Task.WhenAll(globalCommandsTask).ConfigureAwait(false);
                slashCommands = globalCommandsTask.Result
                    .Where(ac => !ac.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(ac => ac.Name).Select(x => x.First())
                    .Where(ac => ac.Name.StartsWith(context.Options[0].Value.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var options = slashCommands.Take(25).Select(sc => new DiscordApplicationCommandAutocompleteChoice(sc.Name, sc.Name.Trim())).ToList();
            return options.AsEnumerable();
        }
    }
}
