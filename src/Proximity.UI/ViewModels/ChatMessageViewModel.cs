namespace Proximity.UI.ViewModels;

/// <summary>
/// Represents a chat message in the session view
/// </summary>
public class ChatMessageViewModel : ViewModelBase
{
    private string _senderName = string.Empty;
    private string _content = string.Empty;
    private DateTime _timestamp;
    private bool _isSystem;

    public string SenderName
    {
        get => _senderName;
        set => SetProperty(ref _senderName, value);
    }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    public bool IsSystem
    {
        get => _isSystem;
        set => SetProperty(ref _isSystem, value);
    }

    public string TimeDisplay => Timestamp.ToLocalTime().ToString("HH:mm");
}
