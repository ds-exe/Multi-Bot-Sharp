namespace Multi_Bot_Sharp.Modules
{
    public class QueueModule
    {
        private static Dictionary<ulong, Queue> players = new Dictionary<ulong, Queue>();

        public Queue? GetQueue(ulong queueId)
        {
            var success = players.TryGetValue(queueId, out var queue);

            if (success)
            {
                return queue;
            }

            return null;
        }

        public Queue AddQueue(ulong queueId)
        {
            var queue = new Queue();
            players.TryAdd(queueId, queue);
            return queue;
        }

        public void RemoveQueue(ulong queueId)
        {
            players.Remove(queueId);
        }
    }
}
