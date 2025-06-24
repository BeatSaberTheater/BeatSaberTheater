using System;
using BeatSaberMarkupLanguage.Settings;
using Zenject;

namespace BeatSaberTheater.Menu;

internal class SettingsMenuManager : IInitializable, IDisposable
{
    private readonly ExampleSettingsMenu exampleSettingsMenu;
    private readonly BSMLSettings bsmlSettings;

    private const string MenuName = nameof(BeatSaberTheater);

    private const string ResourcePath = nameof(BeatSaberTheater) + ".Menu.example.bsml";

    // Zenject will inject our ExampleSettingsMenu instance on this object's creation.
    // BSMLSettings is bound by BSML. SiraUtil also lets us inject services from other mods.
    public SettingsMenuManager(ExampleSettingsMenu exampleSettingsMenu, BSMLSettings bsmlSettings)
    {
        this.exampleSettingsMenu = exampleSettingsMenu;
        this.bsmlSettings = bsmlSettings;
    }

    // Zenject will call IInitializable.Initialize for any menu bindings when the main menu loads for the first
    // time or when the game restarts internally, such as when settings are applied.
    public void Initialize()
    {
        // Adds a custom menu in the Mod Settings section of the main menu.
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, exampleSettingsMenu);
    }

    // Zenject will call IDisposable.Dispose for any menu bindings when the menu scene unloads.
    public void Dispose()
    {
        bsmlSettings.RemoveSettingsMenu(exampleSettingsMenu);
    }
}