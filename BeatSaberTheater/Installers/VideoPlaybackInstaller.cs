using BeatSaberTheater.Environment.Factories;
using BeatSaberTheater.Environment.Interfaces;
using BeatSaberTheater.Playback;
using BeatSaberTheater.Screen;
using BeatSaberTheater.Screen.Factories;
using BeatSaberTheater.Screen.Interfaces;
using BeatSaberTheater.Video;
using Zenject;

namespace BeatSaberTheater.Installers;

// An installer is where related bindings are grouped together. A binding sets up an object for injection.
// Zenject will handle object creation and figure out what needs to be injected automatically.
// It's recommended to check the Zenject documentation to learn more about dependency injection and why it exists.
// https://github.com/Mathijs-Bakker/Extenject?tab=readme-ov-file#what-is-dependency-injection

// This particular installer relates to bindings that are used during Beat Saber's initialization, and are made
// available in any context, whether that be in the menu, or during a map.
// It is related to the PCAppInit installer in the base game.

internal class VideoPlaybackInstaller : Installer
{
    public override void InstallBindings()
    {
        // Playback Dependencies
        Container.BindInterfacesAndSelfTo<VideoLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<SongPreviewPlayerLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<EasingHandler>().AsSingle();

        // Component Factories
        Container.Bind<ICurvedSurfaceFactory>().To<CurvedSurfaceFactory>().AsTransient();
        Container.Bind<ICustomBloomPrePassFactory>().To<CustomBloomPrePassFactory>().AsTransient();
        Container.Bind<ICustomVideoPlayerFactory>().To<CustomVideoPlayerFactory>().AsTransient();
        Container.Bind<ILightManagerFactory>().To<LightManagerFactory>().AsTransient();

        Container.BindInterfacesAndSelfTo<PlaybackManagerPatchEventMapper>().AsSingle();

        Container.Bind<PlaybackManager>().FromNewComponentOnNewGameObject().AsSingle();
    }
}