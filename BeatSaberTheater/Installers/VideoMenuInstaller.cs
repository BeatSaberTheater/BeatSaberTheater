using BeatSaberTheater.Affinity;
using BeatSaberTheater.Services;
using BeatSaberTheater.VideoMenu;
using Zenject;

namespace BeatSaberTheater.Installers;

public class VideoMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        // System initialization
        Container.BindInterfacesTo<MainFlowCoordinatorDidActivatePatch>().AsSingle();

        // Video player and playback manager dependencies
        Container.BindInterfacesAndSelfTo<DownloadService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SearchService>().AsSingle();
        Container.Bind<VideoMenuUI>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<LevelDetailViewController>().FromNewComponentAsViewController().AsSingle();
    }
}