namespace BeatSaberTheater.Util;

public enum DownloadState
{
    NotDownloaded,
    Preparing,
    Downloading,
    DownloadingVideo,
    DownloadingAudio,
    Converting,
    Downloaded,
    Cancelled
}