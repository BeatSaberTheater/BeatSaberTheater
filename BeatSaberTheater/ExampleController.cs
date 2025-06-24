using System;
using UnityEngine;
using Zenject;

namespace BeatSaberTheater;

internal class ExampleController : IInitializable, IDisposable
{
    private readonly ColorSchemesSettings colorSchemesSettings;

    // Zenject will inject the ColorSchemeSettings instance on creation automatically
    public ExampleController(ColorSchemesSettings colorSchemesSettings)
    {
        this.colorSchemesSettings = colorSchemesSettings;
    }

    /// <summary>
    /// Zenject calls IInitializable.Initialize on the first frame the object is created a single time 
    /// </summary>
    public void Initialize()
    {
        Plugin.Log.Info($"{nameof(ExampleController)} initialized");

        var selectedColorScheme = colorSchemesSettings.GetSelectedColorScheme();

        // Logs info about the player's current color scheme
        Plugin.Log.Info(FormatColorScheme(selectedColorScheme));
    }

    /// <summary>
    /// Zenject calls IDisposable.Dispose when, in this case, the application unloads
    /// </summary>
    public void Dispose()
    {
        Plugin.Log.Debug($"{nameof(ExampleController)} disposed");
    }

    private static string FormatColorScheme(ColorScheme colorScheme) =>
        "Displaying the currently selected color scheme:\n" +
        "--------------|-------------------\n" +
        $"Left Saber:    {ToRGBString(colorScheme.saberAColor)}\n" +
        $"Right Saber:   {ToRGBString(colorScheme.saberBColor)}\n" +
        $"Walls Color:   {ToRGBString(colorScheme.obstaclesColor)}\n" +
        $"Env Color A:   {ToRGBString(colorScheme.environmentColor0)}\n" +
        $"Env Color B:   {ToRGBString(colorScheme.environmentColor1)}\n" +
        $"Env Color W:   {ToRGBString(colorScheme.environmentColorW)}\n" +
        $"Boost Color A: {ToRGBString(colorScheme.environmentColor0Boost)}\n" +
        $"Boost Color B: {ToRGBString(colorScheme.environmentColor1Boost)}\n" +
        $"Boost Color W: {ToRGBString(colorScheme.environmentColorWBoost)}";

    private static string ToRGBString(Color color) => $"{color.r:F3}, {color.g:F3}, {color.b:F3}";
}