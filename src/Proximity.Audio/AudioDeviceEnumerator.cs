using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;
using Proximity.Core.Models;

namespace Proximity.Audio;

/// <summary>
/// Enumerates available audio input and output devices on Windows
/// using the Windows Multimedia API (winmm.dll).
/// </summary>
public class AudioDeviceEnumerator : IAudioDeviceEnumerator
{
    private readonly ILogger<AudioDeviceEnumerator> _logger;

    public AudioDeviceEnumerator(ILogger<AudioDeviceEnumerator> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice>();

        try
        {
            int count = NativeMethods.waveInGetNumDevs();
            _logger.LogDebug("Found {Count} input devices", count);

            for (int i = 0; i < count; i++)
            {
                var caps = new NativeMethods.WAVEINCAPS();
                var result = NativeMethods.waveInGetDevCaps((uint)i, ref caps, Marshal.SizeOf(caps));
                if (result == 0)
                {
                    devices.Add(new AudioDevice(
                        id: i.ToString(),
                        name: caps.szPname,
                        isInput: true,
                        isOutput: false));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate input devices");
        }

        return devices;
    }

    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        var devices = new List<AudioDevice>();

        try
        {
            int count = NativeMethods.waveOutGetNumDevs();
            _logger.LogDebug("Found {Count} output devices", count);

            for (int i = 0; i < count; i++)
            {
                var caps = new NativeMethods.WAVEOUTCAPS();
                var result = NativeMethods.waveOutGetDevCaps((uint)i, ref caps, Marshal.SizeOf(caps));
                if (result == 0)
                {
                    devices.Add(new AudioDevice(
                        id: i.ToString(),
                        name: caps.szPname,
                        isInput: false,
                        isOutput: true));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate output devices");
        }

        return devices;
    }

    public AudioDevice? GetDefaultInputDevice()
    {
        try
        {
            if (NativeMethods.waveInGetNumDevs() > 0)
            {
                var caps = new NativeMethods.WAVEINCAPS();
                if (NativeMethods.waveInGetDevCaps(0, ref caps, Marshal.SizeOf(caps)) == 0)
                {
                    return new AudioDevice("0", caps.szPname, isInput: true, isOutput: false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default input device");
        }

        return null;
    }

    public AudioDevice? GetDefaultOutputDevice()
    {
        try
        {
            if (NativeMethods.waveOutGetNumDevs() > 0)
            {
                var caps = new NativeMethods.WAVEOUTCAPS();
                if (NativeMethods.waveOutGetDevCaps(0, ref caps, Marshal.SizeOf(caps)) == 0)
                {
                    return new AudioDevice("0", caps.szPname, isInput: false, isOutput: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default output device");
        }

        return null;
    }

    /// <summary>
    /// P/Invoke declarations for Windows Multimedia API
    /// </summary>
    private static class NativeMethods
    {
        private const string WinMM = "winmm.dll";

        [DllImport(WinMM)]
        internal static extern int waveInGetNumDevs();

        [DllImport(WinMM)]
        internal static extern int waveOutGetNumDevs();

        [DllImport(WinMM, CharSet = CharSet.Auto)]
        internal static extern int waveInGetDevCaps(uint deviceId, ref WAVEINCAPS caps, int size);

        [DllImport(WinMM, CharSet = CharSet.Auto)]
        internal static extern int waveOutGetDevCaps(uint deviceId, ref WAVEOUTCAPS caps, int size);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
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
