using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;
using Proximity.Core.Models;

namespace Proximity.Audio;

/// <summary>
/// Enumerates audio devices using the Windows Multimedia API (winmm.dll).
/// Falls back to a single default device if enumeration fails or the platform is unsupported.
/// </summary>
public class AudioDeviceEnumerator : IAudioDeviceEnumerator
{
    private const uint MMSYSERR_NOERROR = 0;

    private readonly ILogger<AudioDeviceEnumerator> _logger;

    public AudioDeviceEnumerator(ILogger<AudioDeviceEnumerator> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice>
        {
            new AudioDevice
            {
                Id = "default-input",
                Name = "Default Microphone",
                IsDefault = true
            }
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Audio device enumeration is only supported on Windows");
            return devices;
        }

        try
        {
            uint count = NativeMethods.waveInGetNumDevs();
            _logger.LogDebug("Found {Count} wave-in devices", count);

            for (uint i = 0; i < count; i++)
            {
                var caps = new NativeMethods.WAVEINCAPS();
                uint result = NativeMethods.waveInGetDevCapsW(i, ref caps,
                    (uint)Marshal.SizeOf<NativeMethods.WAVEINCAPS>());

                if (result == MMSYSERR_NOERROR)
                {
                    var name = caps.szPname?.Trim('\0') ?? $"Input Device {i + 1}";
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        devices.Add(new AudioDevice
                        {
                            Id = $"wavein-{i}",
                            Name = name,
                            IsDefault = false
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get capabilities for wave-in device {Index}: error {Error}", i, result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating audio input devices");
        }

        return devices;
    }

    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        var devices = new List<AudioDevice>
        {
            new AudioDevice
            {
                Id = "default-output",
                Name = "Default Speakers",
                IsDefault = true
            }
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Audio device enumeration is only supported on Windows");
            return devices;
        }

        try
        {
            uint count = NativeMethods.waveOutGetNumDevs();
            _logger.LogDebug("Found {Count} wave-out devices", count);

            for (uint i = 0; i < count; i++)
            {
                var caps = new NativeMethods.WAVEOUTCAPS();
                uint result = NativeMethods.waveOutGetDevCapsW(i, ref caps,
                    (uint)Marshal.SizeOf<NativeMethods.WAVEOUTCAPS>());

                if (result == MMSYSERR_NOERROR)
                {
                    var name = caps.szPname?.Trim('\0') ?? $"Output Device {i + 1}";
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        devices.Add(new AudioDevice
                        {
                            Id = $"waveout-{i}",
                            Name = name,
                            IsDefault = false
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get capabilities for wave-out device {Index}: error {Error}", i, result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating audio output devices");
        }

        return devices;
    }

    /// <summary>
    /// Native P/Invoke methods for Windows Multimedia API
    /// </summary>
    private static class NativeMethods
    {
        [DllImport("winmm.dll")]
        internal static extern uint waveInGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        internal static extern uint waveInGetDevCapsW(
            uint uDeviceID,
            ref WAVEINCAPS pwic,
            uint cbwic);

        [DllImport("winmm.dll")]
        internal static extern uint waveOutGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        internal static extern uint waveOutGetDevCapsW(
            uint uDeviceID,
            ref WAVEOUTCAPS pwoc,
            uint cbwoc);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WAVEINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
        }
    }
}
