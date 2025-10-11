using BeatSaberTheater.VideoMenu;
using Reactive.BeatSaber;
using SiraUtil.Affinity;

namespace BeatSaberTheater.Affinity;

internal class MainFlowCoordinatorDidActivatePatch : IAffinity
{
    private readonly LevelDetailViewController _viewController;

    public MainFlowCoordinatorDidActivatePatch(LevelDetailViewController viewController)
    {
        _viewController = viewController;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(MainFlowCoordinator), "DidActivate")]
    [AffinityAfter("com.monkeymanboy.BeatSaberMarkupLanguage")]
    private void DidActivatePostfix(MainFlowCoordinator __instance, bool addedToHierarchy)
    {
        if (!addedToHierarchy) return;

        __instance.ReplaceInitialViewControllers(rightScreenViewController: _viewController);
    }
}