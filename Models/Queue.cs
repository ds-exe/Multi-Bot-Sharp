namespace Multi_Bot_Sharp.Models;

public class Queue
{
    private Random _rand = new Random();
    private List<QueueEntry> QueueEntries = new List<QueueEntry>();

    public QueueEntry? PreviousQueueEntry { get; private set; }

    public void AddTrack(DiscordChannel channel, LavalinkTrack track)
    {
        QueueEntries.Add(new QueueEntry(channel, track));
    }

    public QueueEntry? GetNextQueueEntry()
    {
        PreviousQueueEntry = QueueEntries.FirstOrDefault();
        QueueEntries.Remove(PreviousQueueEntry);
        return PreviousQueueEntry;
    }

    public void Shuffle()
    {
        QueueEntries = QueueEntries.OrderBy(_ => _rand.Next()).ToList();
    }
}
