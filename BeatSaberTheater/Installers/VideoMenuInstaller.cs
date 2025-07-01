using BeatSaberTheater.Models;
using BeatSaberTheater.Screen;
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
        Container.BindInterfacesAndSelfTo<LoggingService>().AsSingle();
        Container.Bind<CoroutineStarter>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<VideoLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<DownloadService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SearchService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SongPreviewPlayerLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<EasingHandler>().AsSingle();
        Container.Bind<ICurvedSurfaceFactory>().To<CurvedSurfaceFactory>().AsTransient();
        Container.Bind<CustomVideoPlayer>().FromNewComponentOnNewGameObject().AsSingle();
        Container.Bind<PlaybackController>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<SongPreviewPlayerUpdater>().AsSingle();
        Container.BindInterfacesTo<VideoMenuUI>().AsSingle();
        // Container
        //     .BindInterfacesTo<EnvironmentMaterialsManager.EnvironmentMaterialsManagerInitializer>()
        //     .AsSingle(); // what even is this

        // if (HeckController.DebugMode) Container.BindInterfacesTo<ReloadListener>().AsSingle();
    }
}