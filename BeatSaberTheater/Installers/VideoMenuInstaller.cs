using BeatSaberTheater.Harmony.Patches;
using BeatSaberTheater.Harmony.Signals;
using BeatSaberTheater.Models;
using BeatSaberTheater.Playback;
using BeatSaberTheater.Screen;
using BeatSaberTheater.Screen.Factories;
using BeatSaberTheater.Screen.Interfaces;
using BeatSaberTheater.Services;
using BeatSaberTheater.Util;
using BeatSaberTheater.Video;
using BeatSaberTheater.VideoMenu;
using Zenject;

namespace BeatSaberTheater.Installers;

public class VideoMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        // System initialization
        Container.BindInterfacesAndSelfTo<LoggingService>().AsSingle();

        Container.Bind<CoroutineStarter>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<VideoLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<DownloadService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SearchService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SongPreviewPlayerLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<EasingHandler>().AsSingle();

        // Component Factories
        Container.Bind<ICurvedSurfaceFactory>().To<CurvedSurfaceFactory>().AsTransient();
        Container.Bind<ICustomBloomPrePassFactory>().To<CustomBloomPrePassFactory>().AsTransient();

        Container.Bind<CustomVideoPlayer>().FromNewComponentOnNewGameObject().AsSingle();
        Container.Bind<PlaybackController>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<SongPreviewPlayerUpdater>().AsSingle();
        Container.BindInterfacesTo<VideoMenuUI>().AsSingle();

        Container.QueueForInject(this);
    }
}