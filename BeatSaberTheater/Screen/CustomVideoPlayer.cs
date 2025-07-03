using System;
using System.Diagnostics;
using System.Reflection;
using BeatSaberTheater.Screen.Interfaces;
using BeatSaberTheater.Util;
using BS_Utils.Utilities;
using UnityEngine;
using UnityEngine.Video;
using Zenject;

namespace BeatSaberTheater.Screen;

public class CustomVideoPlayer : MonoBehaviour
{
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly ICurvedSurfaceFactory _curvedSurfaceFactory = null!;
    [Inject] private readonly ICustomBloomPrePassFactory _customBloomPrePassFactory = null!;
    [Inject] private readonly EasingHandler _easingHandler = null!;

    [Inject] private readonly LoggingService _loggingService = null!;
    // [Inject] private readonly PlaybackController _playbackController = null!;
    // [Inject] private readonly VideoMenuUI _videoMenu = null!;

    //Initialized by Awake()
    [NonSerialized] private VideoPlayer _player = null!;
    private AudioSource _videoPlayerAudioSource = null!;
    internal ScreenManager ScreenManager = null!;
    private Renderer _screenRenderer = null!;
    private RenderTexture _renderTexture = null!;

    private const string MAIN_TEXTURE_NAME = "_MainTex";
    private const string CINEMA_TEXTURE_NAME = "_CinemaVideoTexture";
    private const string STATUS_PROPERTY_NAME = "_CinemaVideoIsPlaying";
    private const float MAX_BRIGHTNESS = 0.92f;
    private readonly Color _screenColorOn = Color.white.ColorWithAlpha(0f) * MAX_BRIGHTNESS;
    private readonly Color _screenColorOff = Color.clear;
    private static readonly int MainTex = Shader.PropertyToID(MAIN_TEXTURE_NAME);
    private static readonly int CinemaVideoTexture = Shader.PropertyToID(CINEMA_TEXTURE_NAME);
    private static readonly int CinemaStatusProperty = Shader.PropertyToID(STATUS_PROPERTY_NAME);
    private string _currentlyPlayingVideo = "";
    private readonly Stopwatch _firstFrameStopwatch = new();

    private const float MAX_VOLUME = 0.28f; //Don't ask, I don't know either.
    [NonSerialized] private float _volumeScale = 1.0f;
    private bool _muted = true;
    private bool _bodyVisible;
    private bool _waitingForFadeOut;

    internal event Action? stopped;
    internal event Action<string>? VideoPlayerErrorReceivedEvent;

    public float PanStereo
    {
        set => _videoPlayerAudioSource.panStereo = value;
    }

    public float PlaybackSpeed
    {
        get => _player.playbackSpeed;
        set => _player.playbackSpeed = value;
    }

    public bool PlayerIsPrepared => _player.isPrepared;

    public double PlayerLength => _player.length;

    public double PlayerTime
    {
        get => _player.time;
        set => _player.time = value;
    }

    public float VideoDuration => (float)_player.length;

    public Color ScreenColor
    {
        get => _screenRenderer.material.color;
        set => _screenRenderer.material.color = value;
    }

    public bool VideoEnded { get; private set; }

    public float Volume
    {
        set => _videoPlayerAudioSource.volume = value;
    }

    public string Url
    {
        get => _player.url;
        set => _player.url = value;
    }

    public bool IsPlaying => _player.isPlaying;
    public bool IsFading => _easingHandler.IsFading;
    public bool IsPrepared => _player.isPrepared;
    [NonSerialized] public bool IsSyncing;

    public void Startup(VideoPlayer.FrameReadyEventHandler frameReadyEventHandler,
        VideoPlayer.EventHandler preparedCompleteEventHandler)
    {
        AddFrameReadyEventHandler(frameReadyEventHandler);
        _player.sendFrameReadyEvents = true;
        _player.prepareCompleted += preparedCompleteEventHandler;
    }

    public void Shutdown(VideoPlayer.FrameReadyEventHandler frameReadyEventHandler,
        VideoPlayer.EventHandler preparedCompleteEventHandler)
    {
        RemoveFrameReadyEventHandler(frameReadyEventHandler);
        _player.prepareCompleted -= preparedCompleteEventHandler;
    }

    public void AddFrameReadyEventHandler(VideoPlayer.FrameReadyEventHandler frameReadyEventHandler)
    {
        _player.frameReady += frameReadyEventHandler;
    }

    public void RemoveFrameReadyEventHandler(VideoPlayer.FrameReadyEventHandler frameReadyEventHandler)
    {
        _player.frameReady -= frameReadyEventHandler;
    }

    public void SetVolumeScale(float volume)
    {
        _volumeScale = volume;
    }

    public void UnloadVideo()
    {
        _player.url = null;
        _player.Prepare();
    }

    #region Unity Event Functions

    public void Awake()
    {
        CreateScreen();
        _screenRenderer = ScreenManager.GetRenderer();
        _screenRenderer.material = new Material(GetShader()) { color = _screenColorOff };
        _screenRenderer.material.enableInstancing = true;

        _player = gameObject.AddComponent<VideoPlayer>();
        _player.source = VideoSource.Url;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _renderTexture = ScreenManager.CreateRenderTexture();
        _renderTexture.wrapMode = TextureWrapMode.Mirror;
        _player.targetTexture = _renderTexture;

        _player.playOnAwake = false;
        _player.waitForFirstFrame = true;
        _player.errorReceived += VideoPlayerErrorReceived;
        _player.prepareCompleted += VideoPlayerPrepareComplete;
        _player.started += VideoPlayerStarted;
        _player.loopPointReached += VideoPlayerFinished;

        //TODO PanStereo does not work as expected with this AudioSource. Panning fully to one side is still slightly audible in the other.
        _videoPlayerAudioSource = gameObject.AddComponent<AudioSource>();
        _player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _player.SetTargetAudioSource(0, _videoPlayerAudioSource);
        Mute();
        ScreenManager.SetScreensActive(false);
        LoopVideo(false);

        _videoPlayerAudioSource.reverbZoneMix = 0f;
        _videoPlayerAudioSource.playOnAwake = false;
        _videoPlayerAudioSource.spatialize = false;

        ScreenManager.EnableColorBlending(true);
        _easingHandler.EasingUpdate += FadeHandlerUpdate;
        Hide();

        BSEvents.menuSceneLoaded += OnMenuSceneLoaded;
        SetDefaultMenuPlacement();
    }

    public void OnDestroy()
    {
        BSEvents.menuSceneLoaded -= OnMenuSceneLoaded;
        _easingHandler.EasingUpdate -= FadeHandlerUpdate;
        _renderTexture.Release();
    }

    #endregion

    private void CreateScreen()
    {
        ScreenManager = new ScreenManager(_config, _curvedSurfaceFactory, _customBloomPrePassFactory, _loggingService);
        ScreenManager.CreateScreen(transform);
        ScreenManager.SetScreensActive(true);
        SetDefaultMenuPlacement();
    }

    private static Shader GetShader(string? path = null)
    {
        AssetBundle myLoadedAssetBundle;
        if (path == null)
        {
            var bundle = BeatSaberMarkupLanguage.Utilities.GetResource(Assembly.GetExecutingAssembly(),
                "BeatSaberTheater.Resources.bstheater.bundle");
            if (bundle == null || bundle.Length == 0)
            {
                Plugin._log.Error("GetResource failed");
                return Shader.Find("Hidden/BlitAdd");
            }

            myLoadedAssetBundle = AssetBundle.LoadFromMemory(bundle);
            if (myLoadedAssetBundle == null)
            {
                Plugin._log.Error("LoadFromMemory failed");
                return Shader.Find("Hidden/BlitAdd");
            }
        }
        else
        {
            myLoadedAssetBundle = AssetBundle.LoadFromFile(path);
        }

        var shader = myLoadedAssetBundle.LoadAsset<Shader>("VideoShader");
        myLoadedAssetBundle.Unload(false);

        return shader;
    }

    public void FadeHandlerUpdate(float value)
    {
        ScreenColor = _screenColorOn * value;
        if (!_muted) Volume = MAX_VOLUME * _volumeScale * value;

        if (value >= 1 && _bodyVisible)
            ScreenManager.SetScreenBodiesActive(true);
        else
            ScreenManager.SetScreenBodiesActive(false);

        if (value == 0 && _player.url == _currentlyPlayingVideo && _waitingForFadeOut) Stop();
    }

    public void OnMenuSceneLoaded()
    {
        SetDefaultMenuPlacement();
    }

    public void SetDefaultMenuPlacement(float? width = null)
    {
        var placement = Placement.MenuPlacement;
        placement.Width = width ?? placement.Height * (21f / 9f);
        SetPlacement(placement);
    }

    public void SetPlacement(Placement placement)
    {
        ScreenManager.SetPlacement(placement);
    }

    private void FirstFrameReady(VideoPlayer player, long frame)
    {
        //This is done because the video screen material needs to be set to white, otherwise no video would be visible.
        //When no video is playing, we want it to be black though to not blind the user.
        //If we set the white color when calling Play(), a few frames of white screen are still visible.
        //So, we wait before the player renders its first frame and then set the color, making the switch invisible.
        FadeIn();
        _firstFrameStopwatch.Stop();
        _loggingService.Debug("Delay from Play() to first frame: " + _firstFrameStopwatch.ElapsedMilliseconds + " ms");
        _firstFrameStopwatch.Reset();
        ScreenManager.SetAspectRatio(GetVideoAspectRatio());
        _player.frameReady -= FirstFrameReady;
    }

    public void SetBrightness(float brightness)
    {
        _easingHandler.Value = brightness;
    }

    public void SetBloomIntensity(float? bloomIntensity)
    {
        ScreenManager.SetBloomIntensity(bloomIntensity);
    }

    internal void LoopVideo(bool loop)
    {
        _player.isLooping = loop;
    }

    public void Show()
    {
        FadeIn(0);
    }

    public void FadeIn(float duration = 0.4f)
    {
        // if (EnvironmentController.IsScreenHidden) return;

        ScreenManager.SetScreensActive(true);
        _waitingForFadeOut = false;
        _easingHandler.EaseIn(duration);
    }

    public void Hide()
    {
        FadeOut(0);
    }

    public void FadeOut(float duration = 0.7f)
    {
        _waitingForFadeOut = true;
        _easingHandler.EaseOut(duration);
    }

    public void ShowScreenBody()
    {
        _bodyVisible = true;
        if (!_easingHandler.IsFading && _easingHandler.IsOne) ScreenManager.SetScreenBodiesActive(true);
    }

    public void HideScreenBody()
    {
        _bodyVisible = false;
        if (!_easingHandler.IsFading) ScreenManager.SetScreenBodiesActive(false);
    }

    public void Play()
    {
        if (_firstFrameStopwatch.IsRunning) return;

        _loggingService.Debug("Starting playback, waiting for first frame...");
        _waitingForFadeOut = false;
        _firstFrameStopwatch.Start();
        _player.frameReady -= FirstFrameReady;
        _player.frameReady += FirstFrameReady;
        _player.Play();
        Shader.SetGlobalInt(CinemaStatusProperty, 1);
    }

    public void Pause()
    {
        _player.Pause();
        _firstFrameStopwatch.Reset();
    }

    public void Stop()
    {
        _loggingService.Debug("Stopping playback");
        _player.Stop();
        stopped?.Invoke();
        SetStaticTexture(null);
        Shader.SetGlobalInt(CinemaStatusProperty, 0);
        ScreenManager.SetScreensActive(false);
        _firstFrameStopwatch.Reset();
    }

    public void Prepare()
    {
        stopped?.Invoke();
        _waitingForFadeOut = false;
        _player.Prepare();
    }

    private void Update()
    {
        if (_player.isPlaying || (_player.isPrepared && _player.isPaused)) SetTexture(_player.texture);
    }

    //For manual invocation instead of the event function
    public void UpdateScreenContent()
    {
        SetTexture(_player.texture);
    }

    private void SetTexture(Texture? texture)
    {
        Shader.SetGlobalTexture(CinemaVideoTexture, texture);
    }

    public void SetCoverTexture(Texture? texture)
    {
        SetTexture(texture);

        if (texture == null) return;

        var placement = Placement.CoverPlacement;
        var width = (float)texture.width / texture.height * placement.Height;
        placement.Width = width;
        SetPlacement(placement);
        FadeIn();
    }

    public void SetStaticTexture(Texture? texture)
    {
        if (texture == null)
        {
            ClearTexture();
            return;
        }

        SetTexture(texture);
        var width = (float)texture.width / texture.height * Placement.MenuPlacement.Height;
        SetDefaultMenuPlacement(width);
        ScreenManager.SetShaderParameters(null);
    }

    public void ClearTexture()
    {
        var rt = RenderTexture.active;
        RenderTexture.active = _renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
        SetTexture(_renderTexture);
    }

    private static void VideoPlayerPrepareComplete(VideoPlayer source)
    {
        Plugin._log.Debug("Video player prepare complete");
        var texture = source.texture;
        Plugin._log.Debug($"Video resolution: {texture.width}x{texture.height}");
    }

    private void VideoPlayerStarted(VideoPlayer source)
    {
        _loggingService.Debug("Video player started event");
        _currentlyPlayingVideo = source.url;
        _waitingForFadeOut = false;
        VideoEnded = false;
    }

    private void VideoPlayerFinished(VideoPlayer source)
    {
        _loggingService.Debug("Video player loop point event");
        if (!_player.isLooping)
        {
            VideoEnded = true;
            SetBrightness(0f);
        }
    }

    private void VideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        if (message == "Can't play movie []")
            //Expected when preparing null source
            return;

        _loggingService.Error("Video player error: " + message);
        VideoPlayerErrorReceivedEvent?.Invoke(message);
    }

    public float GetVideoAspectRatio()
    {
        var texture = _player.texture;
        if (texture != null && texture.width != 0 && texture.height != 0)
        {
            var aspectRatio = (float)texture.width / texture.height;
            return aspectRatio;
        }

        _loggingService.Debug("Using default aspect ratio (texture missing)");
        return 16f / 9f;
    }

    public void Mute()
    {
        _muted = true;
        Volume = 0f;
    }

    public void Unmute()
    {
        _muted = false;
    }

    public void SetSoftParent(Transform? parent)
    {
        if (_config.Enable360Rotation) ScreenManager.SetSoftParent(parent);
    }
}