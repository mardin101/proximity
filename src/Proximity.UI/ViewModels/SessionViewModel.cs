namespace Proximity.UI.ViewModels;

/// <summary>
/// Represents a discovered session in the session browser
/// </summary>
public class SessionViewModel : ViewModelBase
{
    private Guid _sessionId;
    private string _sessionName = string.Empty;
    private string _hostName = string.Empty;
    private string _hostAddress = string.Empty;
    private int _port;
    private int _participantCount;
    private int _maxParticipants;

    public Guid SessionId
    {
        get => _sessionId;
        set => SetProperty(ref _sessionId, value);
    }

    public string SessionName
    {
        get => _sessionName;
        set => SetProperty(ref _sessionName, value);
    }

    public string HostName
    {
        get => _hostName;
        set => SetProperty(ref _hostName, value);
    }

    public string HostAddress
    {
        get => _hostAddress;
        set => SetProperty(ref _hostAddress, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public int ParticipantCount
    {
        get => _participantCount;
        set
        {
            if (SetProperty(ref _participantCount, value))
            {
                OnPropertyChanged(nameof(ParticipantDisplay));
            }
        }
    }

    public int MaxParticipants
    {
        get => _maxParticipants;
        set
        {
            if (SetProperty(ref _maxParticipants, value))
            {
                OnPropertyChanged(nameof(ParticipantDisplay));
            }
        }
    }

    public string ParticipantDisplay => $"{ParticipantCount}/{MaxParticipants}";
}
