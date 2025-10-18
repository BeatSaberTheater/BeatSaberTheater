using UnityEngine;
using Reactive;
using BeatSaberTheater.Video.Config;
using BeatSaberTheater.Screen;
using System.Collections;
using System.IO;
using BeatSaberTheater.Download;
using BeatSaberTheater.Util;
using BeatSaberTheater.Screen.Interfaces;
using UnityEngine.Video;

namespace BeatSaberTheater.VideoMenu.V2.Details;

internal class VideoPreviewComponent : ReactiveComponent
{
    private CustomVideoPlayer? _videoPlayer;
    private IEnumerator? _prepareVideoCoroutine;

    private bool _videoPlayerConstructed = false;
    private RectTransform? _rectTransform;

    private readonly ObservableValue<VideoConfig?> _videoConfig;

    public VideoPreviewComponent()
    {
        _videoConfig = Remember<VideoConfig?>(null);
    }

    public void Play(ICustomVideoPlayerFactory customVideoPlayerFactory, VideoConfig config)
    {
        _videoConfig.Value = config;

        if (!_videoPlayerConstructed)
        {
            _videoPlayer = customVideoPlayerFactory?.Create(_rectTransform!.gameObject);
            _videoPlayer?.Startup((VideoPlayer source, long frameIdx) => { }, (VideoPlayer source) => { }, (string _) => { });
            _videoPlayer?.ScreenMenuLoadedFresh();
            var videoPosition = _rectTransform!.position;
            videoPosition.z = videoPosition.z - 0.3f;
            var videoRotation = new Vector3(0, -18);
            _videoPlayer?.SetPlacement(new Placement(videoPosition, videoRotation, 35, 62, 0f));
            _videoPlayer?.SetPosition(videoPosition);
            _videoPlayer?.ShowScreenBody();
            _videoPlayerConstructed = true;
        }

        if (_videoPlayer != null && !string.IsNullOrEmpty(config.VideoPath))
        {
            PrepareVideo(_videoConfig.Value);
            _videoPlayer.FadeIn();
            _videoPlayer.Play();
        }
    }

    public void Stop()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.Stop();
        }
    }

    protected override void Construct(RectTransform rect)
    {
        _rectTransform = rect;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_videoPlayer != null)
        {
            _videoPlayer.Stop();
        }

        // if (_renderTexture != null)
        // {
        //     _renderTexture.Release();
        //     Object.Destroy(_renderTexture);
        // }
    }

    #region Video Prepare

    public void PrepareVideo(VideoConfig video)
    {
        if (_prepareVideoCoroutine != null) StopCoroutine(_prepareVideoCoroutine);

        _videoPlayer?.ClearTexture();

        _prepareVideoCoroutine = PrepareVideoCoroutine(video);
        StartCoroutine(_prepareVideoCoroutine);
    }

    private IEnumerator PrepareVideoCoroutine(VideoConfig video)
    {
        _videoPlayer!.Pause();
        if (_videoConfig.Value?.DownloadState != DownloadState.Downloaded)
        {
            Plugin._log.Debug("Video is not downloaded, stopping prepare");
            _videoPlayer.FadeOut();
            yield break;
        }

        _videoPlayer.LoopVideo(video.loop == true);
        _videoPlayer.SetScreenShaderParameters(video);
        _videoPlayer.SetBloomIntensity(video.bloom);

        if (video.VideoPath == null)
        {
            Plugin._log.Debug("Video path was null, stopping prepare");
            yield break;
        }

        var videoPath = video.VideoPath;
        Plugin._log.Info($"Loading video: {videoPath}");

        if (video.videoFile != null)
        {
            var videoFileInfo = new FileInfo(videoPath);
            var timeout = new DownloadTimeout(0.25f);
            if (_videoPlayer.Url != videoPath)
                yield return new WaitUntil(() =>
                    !TheaterFileHelpers.IsFileLocked(videoFileInfo) || timeout.HasTimedOut);

            timeout.Stop();
            if (timeout.HasTimedOut && TheaterFileHelpers.IsFileLocked(videoFileInfo))
                Plugin._log.Warn("Video file locked: " + videoPath);
        }

        _videoPlayer.Url = videoPath;
        _videoPlayer.Prepare();
    }

    #endregion
}