namespace Multi_Bot_Sharp.Modules
{
    internal class TimeModule : BaseCommandModule
    {
        [Command("time")]
        public async Task Time(CommandContext ctx, params string[] queryList)
        {
            // need to process timezone with paramter
            await ctx.RespondAsync(DateTime.UtcNow.Timestamp(TimestampFormat.LongDateTime));
        }

        [Command("until")]
        public async Task Until(CommandContext ctx, params string[] queryList)
        {
            // need to process timezone with paramter
            await ctx.RespondAsync(DateTime.UtcNow.Timestamp());
        }

        [Command("now")]
        public async Task Now(CommandContext ctx, string timezone = "utc")
        {
            try
            {
                var zone = TZConvert.GetTimeZoneInfo(timezone);
                await ctx.RespondAsync($"`{TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).ToString("dd MMM yyyy, HH:mm")}`");
            }
            catch (TimeZoneNotFoundException e)
            {
                await ctx.RespondAsync("Invalid timezone");
            }
        }

        public static string GenerateUnixTimeNow()
        {
            return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }
    }
}
