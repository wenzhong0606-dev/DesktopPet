using System;
using System.Runtime.InteropServices;

namespace BichonDesktopPet.App.Services;

/// <summary>
/// Reads only Windows' last-input timestamp. No keyboard hooks or input contents are collected.
/// </summary>
public sealed class WindowsActivityMonitor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint TickCount;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    public TimeSpan? GetIdleDuration()
    {
        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref info))
        {
            return null;
        }

        var elapsedMilliseconds = unchecked((uint)Environment.TickCount - info.TickCount);
        return TimeSpan.FromMilliseconds(elapsedMilliseconds);
    }
}
