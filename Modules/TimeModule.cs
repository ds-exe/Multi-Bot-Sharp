namespace Multi_Bot_Sharp.Modules
{
    internal class TimeModule : BaseCommandModule
    {
        [Command("time")]
        public async Task Time(CommandContext ctx)
        {
            // need to process timezone with paramter
            ctx.RespondAsync(DateTime.UtcNow.Timestamp(TimestampFormat.LongDateTime));
        }

        [Command("until")]
        public async Task Until(CommandContext ctx)
        {
            // need to process timezone with paramter
            ctx.RespondAsync(DateTime.UtcNow.Timestamp());
        }

        public static string GenerateUnixTimeNow()
        {
            return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }
    }
}
