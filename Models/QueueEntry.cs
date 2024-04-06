namespace Multi_Bot_Sharp.Models;

public class QueueEntry
{
    public DiscordChannel Channel { get; set; }

    public LavalinkTrack Track { get; set; }

    public DiscordMessage DiscordMessage { get; set; }

    public QueueEntry(DiscordChannel channel, LavalinkTrack track)
    {
        Channel = channel;
        Track = track;
    }
}
