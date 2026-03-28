using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Proximity.Audio;
using Proximity.Core.Configuration;
using Proximity.Core.Models;
using Proximity.Network;

namespace Proximity.UI.ViewModels;

/// <summary>
/// Application views/states
/// </summary>
public enum AppView
{
    UsernameEntry,
    SessionBrowser,
    InSession
}

/// <summary>
/// Main ViewModel orchestrating the entire application flow
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly NetworkModule _networkModule;
    private readonly AudioModule _audioModule;
    private readonly NetworkSettings _networkSettings;
    private readonly Dispatcher _dispatcher;

    // View state
    private AppView _currentView = AppView.UsernameEntry;
    private string _username = string.Empty;
    private string _sessionName = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool _isMuted;
    private string _chatInput = string.Empty;
    private string _currentSessionName = string.Empty;
    private bool _isHost;
    private AudioDevice? _selectedInputDevice;
    private AudioDevice? _selectedOutputDevice;

    // Collections
    public ObservableCollection<SessionViewModel> DiscoveredSessions { get; } = new();
    public ObservableCollection<ParticipantViewModel> Participants { get; } = new();
    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = new();
    public ObservableCollection<AudioDevice> InputDevices { get; } = new();
    public ObservableCollection<AudioDevice> OutputDevices { get; } = new();

    // Commands
    public ICommand SetUsernameCommand { get; }
    public ICommand CreateSessionCommand { get; }
    public ICommand JoinSessionCommand { get; }
    public ICommand LeaveSessionCommand { get; }
    public ICommand ToggleMuteCommand { get; }
    public ICommand SendChatCommand { get; }
    public ICommand KickParticipantCommand { get; }
    public ICommand RefreshAudioDevicesCommand { get; }

    // Properties
    public AppView CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string SessionName
    {
        get => _sessionName;
        set => SetProperty(ref _sessionName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetProperty(ref _isMuted, value))
            {
                _audioModule.IsMuted = value;
            }
        }
    }

    public string ChatInput
    {
        get => _chatInput;
        set => SetProperty(ref _chatInput, value);
    }

    public string CurrentSessionName
    {
        get => _currentSessionName;
        set => SetProperty(ref _currentSessionName, value);
    }

    public bool IsHost
    {
        get => _isHost;
        set => SetProperty(ref _isHost, value);
    }

    public AudioDevice? SelectedInputDevice
    {
        get => _selectedInputDevice;
        set
        {
            if (SetProperty(ref _selectedInputDevice, value))
            {
                _audioModule.SetInputDevice(value);
                _logger.LogInformation("User selected input device: {DeviceName}", value?.Name ?? "None");
            }
        }
    }

    public AudioDevice? SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set
        {
            if (SetProperty(ref _selectedOutputDevice, value))
            {
                _audioModule.SetOutputDevice(value);
                _logger.LogInformation("User selected output device: {DeviceName}", value?.Name ?? "None");
            }
        }
    }

    public MainViewModel(
        ILogger<MainViewModel> logger,
        NetworkModule networkModule,
        AudioModule audioModule,
        NetworkSettings networkSettings)
    {
        _logger = logger;
        _networkModule = networkModule;
        _audioModule = audioModule;
        _networkSettings = networkSettings;
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        // Initialize commands
        SetUsernameCommand = new AsyncRelayCommand(SetUsernameAsync);
        CreateSessionCommand = new AsyncRelayCommand(CreateSessionAsync);
        JoinSessionCommand = new AsyncRelayCommand(JoinSessionAsync);
        LeaveSessionCommand = new AsyncRelayCommand(LeaveSessionAsync);
        ToggleMuteCommand = new RelayCommand(ToggleMute);
        SendChatCommand = new AsyncRelayCommand(SendChatAsync);
        KickParticipantCommand = new AsyncRelayCommand(KickParticipantAsync);
        RefreshAudioDevicesCommand = new RelayCommand(RefreshAudioDevices);

        // Load available audio devices
        RefreshAudioDevices();
    }

    private async Task SetUsernameAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            StatusMessage = "Username must be between 1 and 32 characters";
            return;
        }

        if (Username.Length > 32)
        {
            StatusMessage = "Username must be between 1 and 32 characters";
            return;
        }

        _logger.LogInformation("Username set to '{Username}'", Username);
        StatusMessage = "Searching for sessions...";
        CurrentView = AppView.SessionBrowser;

        // Start session discovery
        await StartDiscoveryAsync();
    }

    private async Task StartDiscoveryAsync()
    {
        if (_networkModule.Discovery == null) return;

        _networkModule.Discovery.SessionDiscovered += OnSessionDiscovered;
        _networkModule.Discovery.SessionLost += OnSessionLost;

        await _networkModule.Discovery.StartDiscoveryAsync();
        StatusMessage = "Looking for sessions on your network...";
    }

    private async Task StopDiscoveryAsync()
    {
        if (_networkModule.Discovery == null) return;

        _networkModule.Discovery.SessionDiscovered -= OnSessionDiscovered;
        _networkModule.Discovery.SessionLost -= OnSessionLost;

        await _networkModule.Discovery.StopDiscoveryAsync();
    }

    private void OnSessionDiscovered(object? sender, VoiceSession session)
    {
        _dispatcher.Invoke(() =>
        {
            var existing = DiscoveredSessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existing != null)
            {
                existing.ParticipantCount = session.ParticipantCount;
                existing.SessionName = session.SessionName;
            }
            else
            {
                DiscoveredSessions.Add(new SessionViewModel
                {
                    SessionId = session.SessionId,
                    SessionName = session.SessionName,
                    HostName = session.HostName,
                    HostAddress = session.HostAddress,
                    Port = session.Port,
                    ParticipantCount = session.ParticipantCount,
                    MaxParticipants = session.MaxParticipants
                });
            }
        });
    }

    private void OnSessionLost(object? sender, Guid sessionId)
    {
        _dispatcher.Invoke(() =>
        {
            var session = DiscoveredSessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                DiscoveredSessions.Remove(session);
            }
        });
    }

    private async Task CreateSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionName))
        {
            StatusMessage = "Please enter a session name";
            return;
        }

        IsBusy = true;
        StatusMessage = "Creating session...";

        try
        {
            await StopDiscoveryAsync();

            var session = await _networkModule.SessionManager!.CreateSessionAsync(
                SessionName, Username, _networkSettings.Port);

            // Start broadcasting the session
            await _networkModule.Discovery!.StartBroadcastingAsync(session);

            // Start audio transport
            await _networkModule.AudioTransport!.StartAsync(session.Port + 1);

            // Wire up session events
            WireSessionEvents();

            IsHost = true;
            CurrentSessionName = session.SessionName;
            CurrentView = AppView.InSession;
            StatusMessage = $"Hosting session '{session.SessionName}'";

            // Add self to participant list
            UpdateParticipantList(new List<Participant>
            {
                new() { Id = _networkModule.SessionManager.LocalParticipantId, Username = Username, IsHost = true }
            });

            AddSystemMessage($"Session '{session.SessionName}' created. Waiting for others to join...");

            _logger.LogInformation("Created session '{SessionName}'", session.SessionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            StatusMessage = $"Failed to create session: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task JoinSessionAsync(object? parameter)
    {
        if (parameter is not SessionViewModel sessionVm)
        {
            StatusMessage = "Please select a session to join";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Joining '{sessionVm.SessionName}'...";

        try
        {
            await StopDiscoveryAsync();

            var session = new VoiceSession
            {
                SessionId = sessionVm.SessionId,
                SessionName = sessionVm.SessionName,
                HostName = sessionVm.HostName,
                HostAddress = sessionVm.HostAddress,
                Port = sessionVm.Port,
                MaxParticipants = sessionVm.MaxParticipants
            };

            // Wire up session events before joining
            WireSessionEvents();

            var success = await _networkModule.SessionManager!.JoinSessionAsync(session, Username);

            if (!success)
            {
                UnwireSessionEvents();
                StatusMessage = "Failed to join session. It may be full or no longer available.";
                await StartDiscoveryAsync();
                return;
            }

            // Start audio transport
            await _networkModule.AudioTransport!.StartAsync(session.Port + 1);

            IsHost = false;
            CurrentSessionName = session.SessionName;
            CurrentView = AppView.InSession;
            StatusMessage = $"Connected to '{session.SessionName}'";

            AddSystemMessage($"Joined '{session.SessionName}'");

            _logger.LogInformation("Joined session '{SessionName}'", session.SessionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join session");
            StatusMessage = $"Failed to join: {ex.Message}";
            await StartDiscoveryAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LeaveSessionAsync()
    {
        _logger.LogInformation("Leaving session");

        try
        {
            UnwireSessionEvents();

            if (_networkModule.Discovery != null)
                await _networkModule.Discovery.StopBroadcastingAsync();

            if (_networkModule.AudioTransport != null)
                await _networkModule.AudioTransport.StopAsync();

            if (_networkModule.SessionManager != null)
                await _networkModule.SessionManager.LeaveSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during leave");
        }

        _dispatcher.Invoke(() =>
        {
            Participants.Clear();
            ChatMessages.Clear();
            DiscoveredSessions.Clear();
            IsHost = false;
            IsMuted = false;
            CurrentView = AppView.SessionBrowser;
            StatusMessage = "Looking for sessions on your network...";
        });

        // Restart discovery
        await StartDiscoveryAsync();
    }

    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _ = _networkModule.SessionManager?.SendMuteStateAsync(IsMuted);
        _logger.LogInformation("Mute toggled: {IsMuted}", IsMuted);
    }

    private void RefreshAudioDevices()
    {
        try
        {
            var inputDevices = _audioModule.GetInputDevices();
            var outputDevices = _audioModule.GetOutputDevices();

            InputDevices.Clear();
            foreach (var device in inputDevices)
            {
                InputDevices.Add(device);
            }

            OutputDevices.Clear();
            foreach (var device in outputDevices)
            {
                OutputDevices.Add(device);
            }

            // Restore selection or select default
            var previousInputId = SelectedInputDevice?.Id;
            var previousOutputId = SelectedOutputDevice?.Id;

            if (previousInputId != null)
            {
                SelectedInputDevice = InputDevices.FirstOrDefault(d => d.Id == previousInputId);
            }
            if (SelectedInputDevice == null && InputDevices.Count > 0)
            {
                SelectedInputDevice = InputDevices[0];
            }

            if (previousOutputId != null)
            {
                SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.Id == previousOutputId);
            }
            if (SelectedOutputDevice == null && OutputDevices.Count > 0)
            {
                SelectedOutputDevice = OutputDevices[0];
            }

            _logger.LogInformation("Refreshed audio devices: {InputCount} input(s), {OutputCount} output(s)",
                InputDevices.Count, OutputDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh audio devices");
        }
    }

    private async Task SendChatAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput)) return;

        var content = ChatInput.Trim();
        ChatInput = string.Empty;

        try
        {
            await _networkModule.SessionManager!.SendChatMessageAsync(content);

            // Add to local chat (for client; host already handles this in SessionManager)
            if (!_networkModule.SessionManager.IsHost)
            {
                _dispatcher.Invoke(() =>
                {
                    ChatMessages.Add(new ChatMessageViewModel
                    {
                        SenderName = Username,
                        Content = content,
                        Timestamp = DateTime.UtcNow
                    });
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send chat message");
        }
    }

    private async Task KickParticipantAsync(object? parameter)
    {
        if (!IsHost) return;

        if (parameter is ParticipantViewModel participant && !participant.IsSelf)
        {
            await _networkModule.SessionManager!.KickParticipantAsync(participant.Id, "Kicked by host");
            AddSystemMessage($"{participant.Username} was kicked from the session");
        }
    }

    private void WireSessionEvents()
    {
        var sm = _networkModule.SessionManager!;
        sm.ParticipantJoined += OnParticipantJoined;
        sm.ParticipantLeft += OnParticipantLeft;
        sm.ParticipantListUpdated += OnParticipantListUpdated;
        sm.ChatMessageReceived += OnChatMessageReceived;
        sm.Kicked += OnKicked;
        sm.Disconnected += OnDisconnected;
    }

    private void UnwireSessionEvents()
    {
        var sm = _networkModule.SessionManager;
        if (sm == null) return;
        sm.ParticipantJoined -= OnParticipantJoined;
        sm.ParticipantLeft -= OnParticipantLeft;
        sm.ParticipantListUpdated -= OnParticipantListUpdated;
        sm.ChatMessageReceived -= OnChatMessageReceived;
        sm.Kicked -= OnKicked;
        sm.Disconnected -= OnDisconnected;
    }

    private void OnParticipantJoined(object? sender, Participant participant)
    {
        _dispatcher.Invoke(() =>
        {
            AddSystemMessage($"{participant.Username} joined the session");
        });
    }

    private void OnParticipantLeft(object? sender, Guid participantId)
    {
        _dispatcher.Invoke(() =>
        {
            var p = Participants.FirstOrDefault(x => x.Id == participantId);
            if (p != null)
            {
                AddSystemMessage($"{p.Username} left the session");
            }
        });
    }

    private void OnParticipantListUpdated(object? sender, List<Participant> participants)
    {
        _dispatcher.Invoke(() => UpdateParticipantList(participants));
    }

    private void OnChatMessageReceived(object? sender, ChatMessage message)
    {
        _dispatcher.Invoke(() =>
        {
            ChatMessages.Add(new ChatMessageViewModel
            {
                SenderName = message.SenderName,
                Content = message.Content,
                Timestamp = message.Timestamp
            });
        });
    }

    private async void OnKicked(object? sender, string reason)
    {
        try
        {
            await _dispatcher.InvokeAsync(() =>
            {
                AddSystemMessage($"You were kicked: {reason}");
                StatusMessage = $"Kicked from session: {reason}";
            });
            await LeaveSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling kick");
        }
    }

    private async void OnDisconnected(object? sender, string reason)
    {
        try
        {
            await _dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"Disconnected: {reason}";
            });
            await LeaveSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnect");
        }
    }

    private void UpdateParticipantList(List<Participant> participants)
    {
        var localId = _networkModule.SessionManager?.LocalParticipantId ?? Guid.Empty;

        Participants.Clear();
        foreach (var p in participants)
        {
            Participants.Add(new ParticipantViewModel
            {
                Id = p.Id,
                Username = p.Username,
                IsHost = p.IsHost,
                IsMuted = p.IsMuted,
                IsSelf = p.Id == localId,
                Volume = _audioModule.GetParticipantVolume(p.Id)
            });
        }
    }

    private void AddSystemMessage(string content)
    {
        ChatMessages.Add(new ChatMessageViewModel
        {
            SenderName = "System",
            Content = content,
            Timestamp = DateTime.UtcNow,
            IsSystem = true
        });
    }
}
