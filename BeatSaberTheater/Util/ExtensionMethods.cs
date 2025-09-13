using System;
using System.Linq;
using SongCore.Data;

namespace BeatSaberTheater.Util;

public static class ExtensionMethods
{
    public static bool HasTheaterSuggestion(this SongData.DifficultyData difficultyData)
    {
        return difficultyData.additionalDifficultyData._suggestions.Any(suggestion =>
            Plugin.Capability.Any(x => x.Equals(suggestion, StringComparison.InvariantCultureIgnoreCase)));
    }

    public static bool HasTheaterRequirement(this SongData.DifficultyData difficultyData)
    {
        return difficultyData.additionalDifficultyData._requirements.Any(requirement =>
            Plugin.Capability.Any(x => x.Equals(requirement, StringComparison.InvariantCultureIgnoreCase)));
    }

    public static bool HasTheater(this SongData.DifficultyData difficultyData)
    {
        return difficultyData.HasTheaterSuggestion() || difficultyData.HasTheaterRequirement();
    }

    public static bool HasTheaterSuggestionInAnyDifficulty(this SongData songData)
    {
        return songData._difficulties.Any(difficulty => difficulty.HasTheaterSuggestion());
    }

    public static bool HasTheaterRequirementInAnyDifficulty(this SongData songData)
    {
        return songData._difficulties.Any(difficulty => difficulty.HasTheaterRequirement());
    }

    public static bool HasTheaterInAnyDifficulty(this SongData songData)
    {
        return songData.HasTheaterSuggestionInAnyDifficulty() || songData.HasTheaterRequirementInAnyDifficulty();
    }


    /// <summary>
    /// Raises an event, wrapping each delegate in a try/catch.
    /// Exceptions thrown are logged, using <paramref name="eventName"/> to provide the name of the event the exception was thrown from.
    /// Yoinked and adapted from BS_Utils.
    /// </summary>
    public static void InvokeSafe<T>(this Action<T>? e, T arg, string eventName)
    {
        if (e == null) return;

        Action<T>[] handlers = e.GetInvocationList().Select(d => (Action<T>)d).ToArray();
        foreach (var handler in handlers)
            try
            {
                handler.Invoke(arg);
            }
            catch (Exception ex)
            {
                Plugin._log.Error(
                    $"Exception thrown in '{eventName}' handler '{handler.Method.Name}': {ex.Message}");
                Plugin._log.Debug(ex);
            }
    }

    //https://stackoverflow.com/a/4405876
    public static string FirstCharToUpper(this string input)
    {
        return input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input[0].ToString().ToUpper() + input.Substring(1)
        };
    }
}