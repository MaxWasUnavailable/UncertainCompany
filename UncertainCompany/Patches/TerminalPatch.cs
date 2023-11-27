using System.Linq;
using HarmonyLib;

namespace UncertainCompany.Patches;

/// <summary>
///     Harmony patches for the <see cref="Terminal" /> class.
/// </summary>
[HarmonyPatch(typeof(Terminal))]
internal class TerminalPatch
{
    /// <summary>
    ///     Patch the <see cref="Terminal.Awake" /> method to set all moon TerminalNodes' displayPlanetInfo to -1 & remove
    ///     [currentPlanetTime] from the different compatible nouns.
    /// </summary>
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void HideMoonWeatherInfoFromRouteCommand(ref Terminal __instance)
    {
        var keywordsList = __instance.terminalNodes.allKeywords.ToList();

        var routeKeyword = keywordsList.Find(keyword => keyword.word == "route");

        // Loop through all route's compatible nouns.

        foreach (var compatibleNoun in routeKeyword.compatibleNouns)
        {
            if (compatibleNoun.result.displayText.ToLower().Contains("company")) continue;

            compatibleNoun.result.displayPlanetInfo = -1;
            compatibleNoun.result.displayText =
                compatibleNoun.result.displayText.Replace("currently [currentPlanetTime]",
                    "The conditions are uncertain").Replace("It is", "");
        }

        // Update original keywords.
        __instance.terminalNodes.allKeywords = keywordsList.ToArray();
    }

    /// <summary>
    ///     Patch the <see cref="Terminal.Awake" /> method to remove [currentPlanetTime] from the moons command.
    /// </summary>
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void HideMoonWeatherInfoFromMoonsCommand(ref Terminal __instance)
    {
        var keywordsList = __instance.terminalNodes.allKeywords.ToList();

        var moonKeyword = keywordsList.Find(keyword => keyword.word == "moons");

        moonKeyword.specialKeywordResult.displayText =
            moonKeyword.specialKeywordResult.displayText.Replace("[planetTime]", "");

        // Update original keywords.
        __instance.terminalNodes.allKeywords = keywordsList.ToArray();
    }
}