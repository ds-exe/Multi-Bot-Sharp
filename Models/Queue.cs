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

    public void AddTrack(CommandContext ctx, LavalinkTrack track)
    {
        QueueEntries.Add(new QueueEntry(ctx.Channel, ctx.Message.Author, track));
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

    public DiscordUser? GetCurrentTrackUser()
    {
        return PreviousQueueEntry?.User;
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
        if (_player.CurrentTrack != null)
        {
            return;
        }

        try
        {
            if (PreviousQueueEntry?.PlayingMessage?.Id != null)
            {
                await PreviousQueueEntry.PlayingMessage.DeleteAsync();
            }
        }
        catch { }

        var next = GetNextQueueEntry();
        if (next == null)
        {
            _queueService.SetLastPlayed(_player.ChannelId);
            StartTimeout();
            return;
        }
        await _player.PlayAsync(next.Track);
        if (PreviousQueueEntry != null)
        {
            PreviousQueueEntry.PlayingMessage = await next.Channel.SendMessageAsync(EmbedHelper.GetTrackPlayingEmbed(next.Track.Info));
        }
    }

    protected async void StartTimeout()
    {
        await Task.Delay(timeoutMinutes * 60 * 1000);
        if (_queueService.GetLastPlayed(_player.ChannelId) <= DateTime.UtcNow.AddMinutes(-timeoutMinutes))
        {
            if (_player.CurrentTrack == null)
            {
                _queueService.RemoveLastPlayed(_player.ChannelId);
                _queueService.RemoveQueue(_player.ChannelId);
                await _player.DisconnectAsync();
            }
        }
    }
}
