using System.Collections.ObjectModel;
using System.Windows.Input;
using Proximity.Core.Models;

namespace Proximity.UI.ViewModels;

/// <summary>
/// Represents the display state of a participant in the session
/// </summary>
public class ParticipantViewModel : ViewModelBase
{
    private Guid _id;
    private string _username = string.Empty;
    private bool _isHost;
    private bool _isMuted;
    private bool _isLocallyMuted;
    private float _volume = 1.0f;
    private bool _isSpeaking;
    private bool _isSelf;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public bool IsHost
    {
        get => _isHost;
        set => SetProperty(ref _isHost, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    public bool IsLocallyMuted
    {
        get => _isLocallyMuted;
        set => SetProperty(ref _isLocallyMuted, value);
    }

    public float Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        set => SetProperty(ref _isSpeaking, value);
    }

    public bool IsSelf
    {
        get => _isSelf;
        set => SetProperty(ref _isSelf, value);
    }

    /// <summary>
    /// Display string for the participant role
    /// </summary>
    public string RoleDisplay => IsHost ? "👑 Host" : "👤 Member";
}
