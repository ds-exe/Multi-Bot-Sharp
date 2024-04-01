namespace Multi_Bot_Sharp.Modules
{
    internal class ResinModule : BaseCommandModule
    {
        private readonly Dictionary<string, Game> games = new Dictionary<string, Game>()
        {
            { "hsr", new Game{ MaxResin = 240, ResinsMins = 6 } },
            { "genshin", new Game{ MaxResin = 160, ResinsMins = 8 } },
        };

        [Command("resin")]
        public async Task Resin(CommandContext ctx, params string[] queryArray)
        {
            if (queryArray.Length == 0)
            {
                await ctx.RespondAsync("No parameter given");
                return;
            }

            if (queryArray[0].ToLower() == "help")
            {
                await ctx.RespondAsync("Help message for resin");
                return;
            }

            ResinData resinData = GetResinData(queryArray);

            if (resinData.Game == null || resinData.Account == null)
            {
                await ctx.RespondAsync("Resin error message");
                return;
            }

            // need to continue processing
            await ctx.RespondAsync("Pass");
        }

        public ResinData GetResinData(string[] queryArray)
        {
            ResinData data = new ResinData();

            var matches = Regex.Match(queryArray[0],@"^([A-z]+)[0-9]?$");

            if (!matches.Success)
            {
                return data;
            }

            var account = matches.Groups[0].Value;
            var game = matches.Groups[1].Value;

            if (!games.ContainsKey(game))
            {
                return data;
            }

            data.Account = account;
            data.Game = game;

            // Need to grab resin data
            return data;
        }



        [Command("hsr")]
        public async Task Hsr(CommandContext ctx, params string[] queryArray)
        {
            await Resin(ctx, queryArray.Prepend("hsr").ToArray());
            return;
        }

        [Command("genshin")]
        public async Task Genshin(CommandContext ctx, params string[] queryArray)
        {
            await Resin(ctx, queryArray.Prepend("genshin").ToArray());
            return;
        }
    }
}
