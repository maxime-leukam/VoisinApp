namespace VoisinApp.Models;

public class Conversation
{
    public string Initials { get; init; } = "";
    public string Name { get; init; } = "";
    public string Preview { get; init; } = "";
    public string Time { get; init; } = "";
    public int UnreadCount { get; init; }

    // Couleur de fond de l'avatar (ex. "#FBE0D8")
    public string AvatarColorHex { get; init; } = "#DCEBE2";

    public bool HasUnread => UnreadCount > 0;
    public Color AvatarColor => Color.FromArgb(AvatarColorHex);
}
