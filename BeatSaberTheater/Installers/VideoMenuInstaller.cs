using BeatSaberTheater.Util;
using BeatSaberTheater.VideoMenu;
using Zenject;

namespace BeatSaberTheater.Installers;

public class VideoMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<LoggingService>().AsSingle();
        Container.BindInterfacesTo<VideoMenuUI>().AsSingle();
        // Container
        //     .BindInterfacesTo<EnvironmentMaterialsManager.EnvironmentMaterialsManagerInitializer>()
        //     .AsSingle(); // what even is this

        // if (HeckController.DebugMode) Container.BindInterfacesTo<ReloadListener>().AsSingle();
    }
}