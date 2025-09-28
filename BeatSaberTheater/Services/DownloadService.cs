﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BeatSaberTheater.Download;
using BeatSaberTheater.Settings;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video;
using BeatSaberTheater.Video.Config;
using IPA.Utilities;
using IPA.Utilities.Async;
using UnityEngine;

namespace BeatSaberTheater.Services;

public class DownloadService : YoutubeDLServiceBase
{
    private readonly ConcurrentDictionary<VideoConfig, Process> _downloadProcesses = new();

    private static readonly Regex DownloadProgressRegex = new(
        @"(?<percentage>\d+\.?\d*)%",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex TranscodeProgressRegex = new(@"out_time_us=(?<micros>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public event Action<VideoConfig>? DownloadProgress;
    public event Action<VideoConfig>? DownloadFinished;

    private readonly string[] _videoHosterWhitelist =
    {
        "https://www.youtube.com/",
        "https://www.dailymotion.com/",
        "https://www.facebook.com/",
        "https://www.bilibili.com/",
        "https://vimeo.com/"
    };

    private readonly TheaterCoroutineStarter _coroutineStarter;
    private readonly VideoLoader _videoLoader;

    public DownloadService(TheaterCoroutineStarter coroutineStarter, LoggingService loggingService,
        VideoLoader videoLoader) : base(loggingService)
    {
        _coroutineStarter = coroutineStarter;
        _videoLoader = videoLoader;
    }

    public void StartDownload(VideoConfig video, VideoQuality.Mode quality, VideoFormats.Format format)
    {
        _coroutineStarter.StartCoroutine(DownloadVideoCoroutine(video, quality, format));
    }

    private IEnumerator DownloadVideoCoroutine(VideoConfig video, VideoQuality.Mode quality, VideoFormats.Format format)
    {
        _loggingService.Info($"Starting download of {video.title}");

        var downloadProcess = CreateDownloadProcess(video, quality, format);
        if (downloadProcess == null)
        {
            _loggingService.Warn("Failed to create download process");
            yield break;
        }

        video.ErrorMessage = null;
        video.DownloadState = DownloadState.Preparing;
        DownloadProgress?.Invoke(video);

        _loggingService.Info(
            $"youtube-dl command: \"{downloadProcess.StartInfo.FileName}\" {downloadProcess.StartInfo.Arguments}");

        var timeout = new DownloadTimeout(5 * 60);

        downloadProcess.OutputDataReceived += (sender, e) =>
            UnityMainThreadTaskScheduler.Factory.StartNew(delegate
            {
                DownloadOutputDataReceived((Process)sender, e, video);
            });

        downloadProcess.ErrorDataReceived += (sender, e) =>
            UnityMainThreadTaskScheduler.Factory.StartNew(delegate { DownloadErrorDataReceived(e, video); });

        downloadProcess.Exited += (sender, e) =>
            UnityMainThreadTaskScheduler.Factory.StartNew(delegate { DownloadProcessExited((Process)sender, video); });

        downloadProcess.Disposed += (sender, e) =>
            UnityMainThreadTaskScheduler.Factory.StartNew(delegate { DownloadProcessDisposed((Process)sender, e); });

        StartProcessThreaded(downloadProcess);
        var startProcessTimeout = new DownloadTimeout(10);
        yield return new WaitUntil(() => IsProcessRunning(downloadProcess) || startProcessTimeout.HasTimedOut);
        startProcessTimeout.Stop();

        yield return new WaitUntil(() => !IsProcessRunning(downloadProcess) || timeout.HasTimedOut);
        if (timeout.HasTimedOut)
            _loggingService.Warn($"[{downloadProcess.Id}] Timeout reached, disposing download process");
        else
            //When the download is finished, wait for process to exit instead of immediately killing it
            yield return new WaitForSeconds(20f);

        timeout.Stop();
        DisposeProcess(downloadProcess);
    }

    private void DownloadOutputDataReceived(Process process, DataReceivedEventArgs eventArgs, VideoConfig video)
    {
        if (!IsProcessRunning(process) || video.DownloadState == DownloadState.Downloaded) return;

        _loggingService.Debug(eventArgs.Data);
        ParseDownloadProgress(video, eventArgs);
        DownloadProgress?.Invoke(video);
    }

    private void DownloadErrorDataReceived(DataReceivedEventArgs eventArgs, VideoConfig video)
    {
        var error = eventArgs.Data.Trim();
        if (error.Length == 0) return;

        _loggingService.Error(error);
        video.ErrorMessage = ShortenErrorMessage(error);
    }

    private void DownloadProcessExited(Process process, VideoConfig video)
    {
        var exitCode = process.ExitCode;
        if (exitCode != 0) video.DownloadState = DownloadState.NotDownloaded;

        _loggingService.Info($"[{process.Id}] Download process exited with code {exitCode}");

        if (video.DownloadState == DownloadState.Cancelled || video.DownloadState == DownloadState.NotDownloaded)
        {
            _loggingService.Info("Cancelled download");
            _videoLoader.DeleteVideo(video);
            DownloadFinished?.Invoke(video);
        }
        else
        {
            process.Disposed -= DownloadProcessDisposed;
            _downloadProcesses.TryRemove(video, out _);
            video.DownloadState = DownloadState.Downloaded;
            video.ErrorMessage = null;
            video.NeedsToSave = true;
            _coroutineStarter.StartCoroutine(WaitForDownloadToFinishCoroutine(video));
            _loggingService.Info($"Download of {video.title} finished");
        }

        DisposeProcess(process);
    }

    private void DownloadProcessDisposed(object sender, EventArgs eventArgs)
    {
        var disposedProcess = (Process)sender;
        foreach (var dictionaryEntry in _downloadProcesses.Where(keyValuePair => keyValuePair.Value == disposedProcess)
                     .ToList())
        {
            var video = dictionaryEntry.Key;
            var success = _downloadProcesses.TryRemove(dictionaryEntry.Key, out _);
            if (!success)
            {
                _loggingService.Error("Failed to remove disposed process from list of processes!");
            }
            else
            {
                video.DownloadState = DownloadState.NotDownloaded;
                DownloadFinished?.Invoke(video);
            }
        }
    }

    private static void ParseDownloadProgress(VideoConfig video, DataReceivedEventArgs dataReceivedEventArgs)
    {
        if (dataReceivedEventArgs.Data == null) return;

        var match = DownloadProgressRegex.Match(dataReceivedEventArgs.Data);
        if (!match.Success)
        {
            if (dataReceivedEventArgs.Data.Contains("Converting video"))
            {
                video.DownloadState = DownloadState.Converting;
            }
            else if (dataReceivedEventArgs.Data.Contains("[Exec]"))
            {
                // When using the webm format, the conversion will be done by executing ffmpeg after downloading
                // and merging the video. This is currently the only --exec step in yt-dlp. Once we see the [Exec] stage
                // being executed, we may assume the conversion has started
                video.DownloadState = DownloadState.Converting;
            }
            else if (dataReceivedEventArgs.Data.Contains("[download]"))
            {
                if (dataReceivedEventArgs.Data.EndsWith(".mp4"))
                    video.DownloadState = DownloadState.DownloadingVideo;
                else if (dataReceivedEventArgs.Data.EndsWith(".m4a"))
                    video.DownloadState = DownloadState.DownloadingAudio;
                else
                    video.DownloadState = DownloadState.Downloading;
            }
            else
            {
                // Try and retrieve transcoding progress from FFMpeg out of stdout. If present, calculate the current
                // transcoding progress by comparing the number of processed seconds to the total video seconds
                var transcodeOutputTimeMatch = TranscodeProgressRegex.Match(dataReceivedEventArgs.Data);
                if (transcodeOutputTimeMatch.Success && long.TryParse(transcodeOutputTimeMatch.Groups["micros"].Value, out var microsConverted))
                {
                    video.ConvertingProgress = ((float)microsConverted / ((long) video.duration * 1000000)) * 100;
                }
            }

            return;
        }

        var ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        ci.NumberFormat.NumberDecimalSeparator = ".";

        video.DownloadProgress =
            float.Parse(match.Groups["percentage"].Value, ci) / 100;
    }

    private IEnumerator WaitForDownloadToFinishCoroutine(VideoConfig video)
    {
        var timeout = new DownloadTimeout(3);
        yield return new WaitUntil(() => timeout.HasTimedOut || File.Exists(video.VideoPath));

        DownloadFinished?.Invoke(video);
    }

    private Process? CreateDownloadProcess(VideoConfig video, VideoQuality.Mode quality, VideoFormats.Format format)
    {
        if (video.LevelDir == null || video.VideoPath == null)
        {
            _loggingService.Error("LevelDir was null during download");
            return null;
        }

        var success = _downloadProcesses.TryGetValue(video, out _);
        if (success)
        {
            _loggingService.Warn("Existing process not cleaned up yet. Cancelling download attempt.");
            return null;
        }

        var path = Path.GetDirectoryName(video.VideoPath);
        if (video.VideoPath != null && path != null && !Directory.Exists(path))
        {
            _loggingService.Debug("Creating folder: " + path);
            //Needed for OST/WIP videos
            Directory.CreateDirectory(path);
        }
        else
        {
            _loggingService.Debug("Folder already exists: " + path);
        }

        string videoUrl;
        if (video.videoUrl != null)
        {
            if (UrlInWhitelist(video.videoUrl))
            {
                videoUrl = video.videoUrl;
            }
            else
            {
                _loggingService.Error($"Video hoster for {video.videoUrl} is not allowed");
                return null;
            }
        }
        else if (video.videoID != null)
        {
            videoUrl = $"https://www.youtube.com/watch?v={video.videoID}";
        }
        else
        {
            _loggingService.Error("Video config has neither videoID or videoUrl set");
            return null;
        }

        var videoFormat = VideoQuality.ToYoutubeDLFormat(video, quality);
        videoFormat = videoFormat.Length > 0 ? $" -f \"{videoFormat}\"" : "";
        var outputPath = video.VideoPath;
        
        var downloadProcessArguments = videoUrl +
                                       videoFormat +
                                       " --no-cache-dir" + // Don't use temp storage
                                       $" -o \"{outputPath}\"" +
                                       " --no-playlist" + // Don't download playlists, only the first video
                                       " --no-part" + // Don't store download in parts, write directly to file
                                       " --no-mtime" + //Video last modified will be when it was downloaded, not when it was uploaded to youtube
                                       " --socket-timeout 10"; //Retry if no response in 10 seconds Note: Not if download takes more than 10 seconds but if the time between any 2 messages from the server is 10 seconds

        if (format == VideoFormats.Format.Webm)
        {
            downloadProcessArguments += $" --exec \"{Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe")} -i %(filepath,_filename|)q -progress pipe:1 -c:v libvpx -crf 10 -b:v 4M -quality realtime -cpu-used 8 -c:a libvorbis \"\"{Path.GetFileNameWithoutExtension(video.VideoPath)}.webm\"\"\"";
            video.videoFile = Path.GetFileNameWithoutExtension(video.videoFile) + ".webm";
        }
        else
        {
            downloadProcessArguments += " --recode-video mp4"; //Re-encode to mp4 (will be skipped most of the time, since it's already in an mp4 container)
        }

        var process = CreateProcess(downloadProcessArguments, video.LevelDir);
        if (format == VideoFormats.Format.Webm)
        {
            // Clean up download .mp4 file when using Webm. It is not used, and nearly doubles the size on disk of the video files
            process.Disposed += ((_, _) =>
            {
                Plugin._log.Info("Cleaning up present mp4 file. Reason: WebM is requested as output format");
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                    Plugin._log.Info("Removed: " + outputPath);
                }
                else
                {
                    Plugin._log.Warn("Video file doesn't exist - cannot remove: " + outputPath);
                }
            });
        }
        
        _downloadProcesses.TryAdd(video, process);
        return process;
    }

    public void CancelDownload(VideoConfig video)
    {
        _loggingService.Debug("Cancelling download");
        video.DownloadState = DownloadState.Cancelled;
        DownloadProgress?.Invoke(video);

        var success = _downloadProcesses.TryGetValue(video, out var process);
        if (success) DisposeProcess(process);

        _videoLoader.DeleteVideo(video);
    }

    private bool UrlInWhitelist(string url)
    {
        return _videoHosterWhitelist.Any(url.StartsWith);
    }

    private static string ShortenErrorMessage(string rawError)
    {
        var error = rawError;
        error = Regex.Replace(error, @"^ERROR: ", "");
        var prefixRegex = new Regex(@"^\[(?<type>[^\]]*)\][^:]*:? (?<msg>.*)$");
        var match = prefixRegex.Match(error);
        string? errorType = null;
        if (match.Success)
        {
            error = match.Groups["msg"].Value;
            errorType = match.Groups["type"].Value;
            if (!string.IsNullOrEmpty(errorType)) errorType = errorType.FirstCharToUpper();
        }

        if (error.Contains("The uploader has not made this video available in your country"))
            error = "Video is geo-restricted";
        else
            error = $"{errorType ?? "Unknown"} error. See log for details.";

        return error;
    }
}