namespace Multi_Bot_Sharp.Modules
{
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

        public static bool IsDM(CommandContext ctx)
        {
            return ctx.Channel.IsPrivate;
        }
    }
}
