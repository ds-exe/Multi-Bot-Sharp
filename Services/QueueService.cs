namespace Multi_Bot_Sharp.Services;

public class QueueService
{
    private static Dictionary<ulong, Queue> players = new Dictionary<ulong, Queue>();

    private static Dictionary<ulong, DateTime> playersLastPlayed = new Dictionary<ulong, DateTime>();

    private Queue? GetQueueInternal(ulong guildId)
    {
        var success = players.TryGetValue(guildId, out var queue);

        if (success)
        {
            return queue;
        }

        return null;
    }

    public Queue? GetQueue(ulong guildId)
    {
        return GetQueueInternal(guildId);
    }

    public Queue AddQueue(ulong guildId, LavalinkGuildPlayer player)
    {
        var queue = GetQueueInternal(guildId);
        if (queue != null)
        {
            return queue;
        }

        queue = new Queue(this, player);
        players[guildId] = queue;
        return queue;
    }

    public void ClearQueue(ulong guildId)
    {
        var queue = GetQueue(guildId);
        if (queue != null)
        {
            queue.ClearQueue();
        }
    }

    public void RemoveQueue(ulong guildId)
    {
        players.Remove(guildId);
    }

    public void SetLastPlayed(ulong guildId)
    {
        playersLastPlayed[guildId] = DateTime.UtcNow;
    }

    public DateTime GetLastPlayed(ulong guildId)
    {
        var success = playersLastPlayed.TryGetValue(guildId, out var lastPlayed);

        if (success)
        {
            return lastPlayed;
        }

        return DateTime.MinValue;
    }

    public void RemoveLastPlayed(ulong guildId)
    {
        playersLastPlayed.Remove(guildId);
    }
}
