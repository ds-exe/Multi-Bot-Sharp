namespace Multi_Bot_Sharp.Services;

public class QueueService
{
    private static Dictionary<ulong, Queue> players = new Dictionary<ulong, Queue>();

    private static Dictionary<ulong, DateTime> playersLastPlayed = new Dictionary<ulong, DateTime>();

    private Queue? GetQueueInternal(ulong queueId)
    {
        var success = players.TryGetValue(queueId, out var queue);

        if (success)
        {
            return queue;
        }

        return null;
    }

    public Queue? GetQueue(ulong queueId)
    {
        return GetQueueInternal(queueId);
    }

    public Queue AddQueue(ulong queueId, LavalinkGuildPlayer player)
    {
        var queue = GetQueueInternal(queueId);
        if (queue != null)
        {
            return queue;
        }

        queue = new Queue(this, player);
        players[queueId] = queue;
        return queue;
    }

    public void ClearQueue(ulong queueId)
    {
        var queue = GetQueue(queueId);
        if (queue != null)
        {
            queue.ClearQueue();
        }
    }

    public void RemoveQueue(ulong queueId)
    {
        players.Remove(queueId);
    }

    public void SetLastPlayed(ulong queueId)
    {
        playersLastPlayed[queueId] = DateTime.UtcNow;
    }

    public DateTime GetLastPlayed(ulong queueId)
    {
        var success = playersLastPlayed.TryGetValue(queueId, out var lastPlayed);

        if (success)
        {
            return lastPlayed;
        }

        return DateTime.MinValue;
    }

    public void RemoveLastPlayed(ulong queueId)
    {
        playersLastPlayed.Remove(queueId);
    }
}
