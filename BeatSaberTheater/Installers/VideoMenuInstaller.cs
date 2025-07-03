using BeatSaberTheater.Services;
using BeatSaberTheater.VideoMenu;
using Zenject;

namespace BeatSaberTheater.Installers;

public class VideoMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        // System initialization

        // Video player and playback manager dependencies
        Container.BindInterfacesAndSelfTo<DownloadService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SearchService>().AsSingle();
        Container.BindInterfacesTo<VideoMenuUI>().AsSingle();
    }
}