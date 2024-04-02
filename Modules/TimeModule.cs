using System;

namespace Multi_Bot_Sharp.Modules
{
    public class TimeModule : BaseCommandModule
    {
        public EmbedModule EmbedModule { private get; set; }

        private Dictionary<string, string> timezones;

        public TimeModule()
        {
            string cwd = Directory.GetCurrentDirectory();
            string path = cwd + @"/timezones.json";

            #if DEBUG
            path = cwd + @"/../../../timezones.json";
            #endif

            string text = File.ReadAllText(path);

            timezones = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
        }

        [Command("time")]
        public async Task Time(CommandContext ctx, [Description("")] DateTime time, string timezone = "utc")
        {
            try
            {
                var zone = TZConvert.GetTimeZoneInfo(timezone);
                var timestamp = TimeZoneInfo.ConvertTimeToUtc(time, zone).Timestamp(TimestampFormat.LongDateTime);
                await ctx.RespondAsync(EmbedModule.GetTimestampEmbed(timestamp));
            }
            catch (TimeZoneNotFoundException e)
            {
                await ctx.RespondAsync("Invalid timezone");
            }
        }

        //[Command("time")]


        [Command("until")]
        public async Task Until(CommandContext ctx, [RemainingText][Description("")] string query)
        {
            // need to process timezone with paramter
            var time = DateTime.UtcNow.Timestamp();
            await ctx.RespondAsync(EmbedModule.GetTimestampEmbed(time));
        }

        [Command("now")]
        [Description("Gets the time for a given timezone")]
        public async Task Now(CommandContext ctx, [Description("Optional timezone, default UTC")] string timezone = "utc")
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
