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
    private const int TicksBetweenWeatherChanges = 10;
    private static int _ticksSinceLastWeatherChange;

    // ReSharper disable once InconsistentNaming
    private static readonly Random _random = new();

    /// <summary>
    ///     Patch the <see cref="StartOfRound.SetMapScreenInfoToCurrentLevel" /> method to hide the weather info from the
    ///     display.
    /// </summary>
    /// <param name="__instance"> The <see cref="StartOfRound" /> instance. </param>
    [HarmonyPatch("SetMapScreenInfoToCurrentLevel")]
    [HarmonyPostfix]
    internal static void HideWeatherInfoFromDisplay(ref StartOfRound __instance)
    {
        if (!UncertainCompany.Instance.DoHideWeather.Value) return;

        UpdateScreenLevelDescription(ref __instance);
    }

    /// <summary>
    ///     Patch the <see cref="StartOfRound.Update" /> method to continuously scramble the weather info.
    /// </summary>
    /// <param name="__instance"> The <see cref="StartOfRound" /> instance. </param>
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    internal static void ScrambleWeatherInfoOnUpdate(ref StartOfRound __instance)
    {
        if (!UncertainCompany.Instance.DoHideWeather.Value) return;

        if (_ticksSinceLastWeatherChange < TicksBetweenWeatherChanges + _random.Next(0, 10))
        {
            _ticksSinceLastWeatherChange++;
            return;
        }

        _ticksSinceLastWeatherChange = 0;

        UpdateScreenLevelDescription(ref __instance);
    }

    private static void UpdateScreenLevelDescription(ref StartOfRound __instance)
    {
        if (__instance.screenLevelDescription.text.Contains("Weather:"))
        {
            __instance.screenLevelDescription.text = Regex.Replace(
                __instance.screenLevelDescription.text, "Weather.*", ScrambledWeatherText());
        }
        else
        {
            if (!__instance.screenLevelDescription.text.EndsWith("\n")) __instance.screenLevelDescription.text += "\n";
            __instance.screenLevelDescription.text += ScrambledWeatherText();
        }
    }

    /// <summary>
    ///     Creates a scrambled weather text.
    /// </summary>
    /// <returns> The scrambled weather text. </returns>
    private static string ScrambledWeatherText()
    {
        var output = "Weather: ";

        var weatherString = Enum.GetValues(typeof(LevelWeatherType)).Cast<LevelWeatherType>().ToArray()[
            _random.Next(0, Enum.GetValues(typeof(LevelWeatherType)).Length)].ToString();

        // Scramble the randomWeather string.
        var weatherStringLength = weatherString.Length;

        for (var i = 0; i < weatherStringLength; i++)
            switch (_random.Next(0, 5))
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
                    output += weatherString[_random.Next(0, weatherStringLength)];
                    break;
                default:
                    // Random from alphanumeric characters
                    output += (char)_random.Next(48, 122);
                    break;
            }

        return output;
    }
}