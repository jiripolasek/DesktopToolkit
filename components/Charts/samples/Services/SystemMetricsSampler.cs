// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.InteropServices;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal sealed class SystemMetricsSampler
{
    private ulong _previousIdleTime;
    private ulong _previousKernelTime;
    private ulong _previousUserTime;
    private double _lastProcessorUsage;
    private bool _hasPreviousProcessorTimes;

    public SystemMetricsSample Read()
    {
        var processorUsage = this.ReadProcessorUsage();
        var memoryStatus = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>(), };

        if (!GlobalMemoryStatusEx(ref memoryStatus))
        {
            return new SystemMetricsSample(processorUsage, 0, 0, 0);
        }

        var memoryUsage = Math.Clamp((double)memoryStatus.MemoryLoad, 0, 100);
        var usedPhysicalMemory = memoryStatus.TotalPhysical - memoryStatus.AvailablePhysical;

        return new SystemMetricsSample(processorUsage, memoryUsage, usedPhysicalMemory, memoryStatus.TotalPhysical);
    }

    private double ReadProcessorUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return this._lastProcessorUsage;
        }

        var idle = ToUInt64(idleTime);
        var kernel = ToUInt64(kernelTime);
        var user = ToUInt64(userTime);

        if (!this._hasPreviousProcessorTimes)
        {
            this._previousIdleTime = idle;
            this._previousKernelTime = kernel;
            this._previousUserTime = user;
            this._hasPreviousProcessorTimes = true;
            return this._lastProcessorUsage;
        }

        var idleDelta = idle - this._previousIdleTime;
        var kernelDelta = kernel - this._previousKernelTime;
        var userDelta = user - this._previousUserTime;
        var totalDelta = kernelDelta + userDelta;

        this._previousIdleTime = idle;
        this._previousKernelTime = kernel;
        this._previousUserTime = user;

        if (totalDelta == 0 || idleDelta > totalDelta)
        {
            return this._lastProcessorUsage;
        }

        this._lastProcessorUsage = Math.Clamp((totalDelta - idleDelta) * 100d / totalDelta, 0, 100);
        return this._lastProcessorUsage;
    }

    private static ulong ToUInt64(FileTime value)
    {
        return ((ulong)value.HighDateTime << 32) | value.LowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}

internal readonly record struct SystemMetricsSample(
    double ProcessorUsage,
    double MemoryUsage,
    ulong UsedPhysicalMemory,
    ulong TotalPhysicalMemory);
