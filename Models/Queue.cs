namespace Multi_Bot_Sharp.Models;

public class Queue
{
    protected const int timeoutMinutes = 15;
    private Random _rand = new Random();
    private List<QueueEntry> QueueEntries = new List<QueueEntry>();
    private QueueEntry? PreviousQueueEntry = null;
    private QueueService _queueService;
    private LavalinkGuildPlayer _player;

    public Queue(QueueService queueService, LavalinkGuildPlayer player)
    {
        _queueService = queueService;
        _player = player;
        player.TrackEnded += Player_TrackEnded;
    }

    public void AddTrack(DiscordChannel channel, LavalinkTrack track)
    {
        QueueEntries.Add(new QueueEntry(channel, track));
    }

    protected QueueEntry? GetNextQueueEntry()
    {
        PreviousQueueEntry = QueueEntries.FirstOrDefault();
        if (PreviousQueueEntry == null)
        {
            return PreviousQueueEntry;
        }
        QueueEntries.Remove(PreviousQueueEntry);
        return PreviousQueueEntry;
    }

    public void Shuffle()
    {
        QueueEntries = QueueEntries.OrderBy(_ => _rand.Next()).ToList();
    }

    protected async Task Player_TrackEnded(LavalinkGuildPlayer sender, DisCatSharp.Lavalink.EventArgs.LavalinkTrackEndedEventArgs e)
    {
        await PlayQueueAsync();
    }

    public async Task PlayQueueAsync()
    {
        if (!_queueService.QueueExists(this))
        {
            return;
        }

        if (_player.CurrentTrack != null)
        {
            return;
        }

        if (PreviousQueueEntry != null)
        {
            try
            {
                if (PreviousQueueEntry.DiscordMessage?.Id != null)
                {
                    await PreviousQueueEntry.DiscordMessage.DeleteAsync();
                }
            }
            catch
            {
                return;
            }
        }

        var next = GetNextQueueEntry();
        if (next == null)
        {
            _queueService.SetLastPlayed(_player.ChannelId);
            Timeout(_player);
            return;
        }
        await _player.PlayAsync(next.Track);
        if (PreviousQueueEntry != null)
        {
            PreviousQueueEntry.DiscordMessage = await next.Channel.SendMessageAsync(EmbedHelper.GetTrackPlayingEmbed(next.Track.Info));
        }
    }

    protected async void Timeout(LavalinkGuildPlayer player)
    {
        await Task.Delay(timeoutMinutes * 60 * 1000);
        if (_queueService.GetLastPlayed(player.ChannelId) <= DateTime.UtcNow.AddMinutes(-timeoutMinutes))
        {
            if (player.CurrentTrack == null)
            {
                _queueService.RemoveLastPlayed(player.ChannelId);
                _queueService.RemoveQueue(player.ChannelId);
                await player.DisconnectAsync();
            }
        }
    }
}
