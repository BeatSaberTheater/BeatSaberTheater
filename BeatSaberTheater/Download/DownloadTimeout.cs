using System;
using System.Diagnostics;

namespace BeatSaberTheater.Download;

public class DownloadTimeout
{
    private readonly Stopwatch _timer;
    private readonly long _timeoutTicks;

    public bool HasTimedOut => _timer.ElapsedTicks >= _timeoutTicks;

    public DownloadTimeout(float timeoutSec)
    {
        _timer = new Stopwatch();
        _timer.Start();
        _timeoutTicks = (long)(timeoutSec * TimeSpan.TicksPerSecond);
    }

    public void Stop()
    {
        _timer.Stop();
    }
}