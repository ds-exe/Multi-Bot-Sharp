namespace Multi_Bot_Sharp.Models
{
    public class Queue
    {
        public List<QueueEntry> QueueEntries = new List<QueueEntry>();

        public  QueueEntry? PreviousQueueEntry { get; set; }

        public void AddTrack(DiscordChannel channel, LavalinkTrack track)
        {
            QueueEntries.Add(new QueueEntry(channel, track));
        }

        public QueueEntry? GetNextQueueEntry()
        {
            PreviousQueueEntry = QueueEntries.FirstOrDefault();
            QueueEntries.RemoveAt(0);
            return PreviousQueueEntry;
        }
    }
}
