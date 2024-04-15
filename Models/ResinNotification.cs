namespace Multi_Bot_Sharp.Models;

public class ResinNotification
{
    public ulong UserId { get; set; }

    public string Game { get; set; }

    public int NotificationResin { get; set; }

    public DateTime NotificationTimestamp { get; set; }

    public DateTime MaxResinTimestamp { get; set; }
}
