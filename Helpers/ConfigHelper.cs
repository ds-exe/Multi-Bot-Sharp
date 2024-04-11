namespace Multi_Bot_Sharp.Helpers;

public class ConfigHelper
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
