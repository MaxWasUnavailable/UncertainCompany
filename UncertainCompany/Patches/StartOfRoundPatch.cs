using System;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace UncertainCompany.Patches;

/// <summary>
///     Harmony patches for the <see cref="StartOfRound" /> class.
/// </summary>
[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatch
{
    // ReSharper disable once InconsistentNaming
    private static readonly Random random = new();
    private static int _ticksSinceLastWeatherChange = 0;
    private static readonly int _ticksBetweenWeatherChanges = 10;

    /// <summary>
    ///     Patch the <see cref="StartOfRound.SetMapScreenInfoToCurrentLevel" /> method to hide the weather info from the
    ///     display.
    /// </summary>
    /// <param name="__instance"> The <see cref="StartOfRound" /> instance. </param>
    [HarmonyPatch("SetMapScreenInfoToCurrentLevel")]
    [HarmonyPostfix]
    internal static void HideWeatherInfoFromDisplay(ref StartOfRound __instance)
    {
        __instance.screenLevelDescription.text = Regex.Replace(
            __instance.screenLevelDescription.text, "\nWeather.*", ScrambledWeatherText());
    }

    /// <summary>
    ///     Patch the <see cref="StartOfRound.Update" /> method to continuously scramble the weather info.
    /// </summary>
    /// <param name="__instance"> The <see cref="StartOfRound" /> instance. </param>
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    internal static void ScrambleWeatherInfoOnUpdate(ref StartOfRound __instance)
    {
        if (_ticksSinceLastWeatherChange < _ticksBetweenWeatherChanges)
        {
            _ticksSinceLastWeatherChange++;
            return;
        }
        _ticksSinceLastWeatherChange = 0;
        
        __instance.screenLevelDescription.text = Regex.Replace(
            __instance.screenLevelDescription.text, "\nWeather.*", ScrambledWeatherText());
    }

    /// <summary>
    ///     Creates a scrambled weather text.
    /// </summary>
    /// <returns> The scrambled weather text. </returns>
    private static string ScrambledWeatherText()
    {
        var output = "\nWeather: ";

        var weatherString = Enum.GetValues(typeof(LevelWeatherType)).Cast<LevelWeatherType>().ToArray()[
            random.Next(0, Enum.GetValues(typeof(LevelWeatherType)).Length)].ToString();

        // Scramble the randomWeather string.
        var weatherStringLength = weatherString.Length;

        for (var i = 0; i < weatherStringLength; i++)
            switch (random.Next(0, 5))
            {
                case 0:
                    // Uppercased
                    output += weatherString[i].ToString().ToUpper();
                    break;
                case 1:
                    // Lowercased
                    output += weatherString[i].ToString().ToLower();
                    break;
                case 2:
                    // Random from the weather string
                    output += weatherString[random.Next(0, weatherStringLength)];
                    break;
                default:
                    // Random from alphanumeric characters
                    output += (char)random.Next(48, 122);
                    break;
            }

        return output;
    }
}