using System;
using System.IO;
using System.Text.Json.Serialization;
using BeatSaberTheater.Util;

namespace BeatSaberTheater.Video;

public class VideoConfig
{
    public bool? allowCustomPlatform;
    public string? author;
    public bool? configByMapper;
    public int duration; //s
    public bool? forceEnvironmentModifications;
    public int offset; //ms
    public string? title;
    public UserSettings? userSettings;
    public string? videoFile;
    public string? videoID;
    public string? videoUrl;

    [JsonIgnore] [NonSerialized] public float DownloadProgress;
    [JsonIgnore] [NonSerialized] public DownloadState DownloadState;
    [JsonIgnore] [NonSerialized] public string? ErrorMessage;
    [JsonIgnore] [NonSerialized] public string? LevelDir;
    [JsonIgnore] [NonSerialized] public bool NeedsToSave;
    [JsonIgnore] [NonSerialized] public bool PlaybackDisabledByMissingSuggestion;

    [JsonIgnore]
    public bool IsDownloading => DownloadState == DownloadState.Preparing ||
                                 DownloadState == DownloadState.Downloading ||
                                 DownloadState == DownloadState.DownloadingVideo ||
                                 DownloadState == DownloadState.DownloadingAudio;

    [JsonIgnore] public bool IsOfficialConfig => configByMapper is true;

    [JsonIgnore]
    public bool IsPlayable => DownloadState == DownloadState.Downloaded &&
                              !PlaybackDisabledByMissingSuggestion;

    [JsonIgnore]
    public string? VideoPath
    {
        get
        {
            if (LevelDir != null)
            {
                var path = Directory.GetParent(LevelDir)!.FullName;
                var mapFolderName = new DirectoryInfo(LevelDir).Name;
                // TODO
                // var folder = Path.Combine(path, VideoLoader.WIP_DIRECTORY_NAME, mapFolderName);
                var folder = Path.Combine(path, mapFolderName);
                videoFile = GetVideoFileName(folder);
                path = Path.Combine(folder, videoFile);
                return path;
            }

            if (LevelDir != null)
                try
                {
                    videoFile = GetVideoFileName(LevelDir);
                    return Path.Combine(LevelDir, videoFile);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to combine video path for {videoFile}: {e.Message}");
                    return null;
                }

            Log.Debug("VideoPath is null");
            return null;
        }
    }

    public DownloadState UpdateDownloadState()
    {
        return DownloadState = VideoPath != null && (videoID != null || videoUrl != null) && File.Exists(VideoPath)
            ? DownloadState.Downloaded
            : DownloadState.NotDownloaded;
    }

    private string GetVideoFileName(string levelPath)
    {
        var fileName = videoFile ?? FileHelpers.ReplaceIllegalFilesystemChars(title ?? videoID ?? "video");
        fileName = FileHelpers.ShortenFilename(levelPath, fileName);
        if (!fileName.EndsWith(".mp4")) fileName += ".mp4";
        return fileName;
    }
}