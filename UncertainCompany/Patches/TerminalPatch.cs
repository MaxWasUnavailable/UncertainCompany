using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace UncertainCompany.Patches;

/// <summary>
///     Harmony patches for the <see cref="Terminal" /> class.
/// </summary>
[HarmonyPatch(typeof(Terminal))]
internal class TerminalPatch
{
    private const double MaxItemSaleAddition = 0.30;
    private const double MaxItemSaleSubtraction = 0.10;
    private const double MaxMoonCostAddition = 0.25;
    private const double MaxMoonCostSubtraction = 0.25;
    private const int Max0MoonCost = 200;
    private static readonly Dictionary<string, int> OriginalMoonCosts = new();

    /// <summary>
    ///     Patch the <see cref="Terminal.Awake" /> method to set all moon TerminalNodes' displayPlanetInfo to -1 & remove
    ///     [currentPlanetTime] from the different compatible nouns.
    /// </summary>
    /// <param name="__instance"> The <see cref="Terminal" /> instance. </param>
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
    /// <param name="__instance"> The <see cref="Terminal" /> instance. </param>
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

    /// <summary>
    ///     Patch the <see cref="Terminal.SetItemSales" /> method to randomise moon travel prices.
    /// </summary>
    /// <param name="__instance"> The <see cref="Terminal" /> instance. </param>
    [HarmonyPatch("SetItemSales")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void RandomiseItemSales(ref Terminal __instance)
    {
        var random = new Random(StartOfRound.Instance.randomMapSeed);

        for (var itemIndex = 0; itemIndex < __instance.itemSalesPercentages.Length; itemIndex++)
        {
            if (__instance.itemSalesPercentages[itemIndex] == 100) continue;

            __instance.itemSalesPercentages[itemIndex] += GetRandomItemSaleOffset(random);
        }
    }

    private static int GetRandomItemSaleOffset(Random random)
    {
        var buyingRateChange = random.Next(0, 2) switch
        {
            0 => random.NextDouble() * MaxItemSaleAddition,
            _ => random.NextDouble() * -MaxItemSaleSubtraction
        };

        return (int)(buyingRateChange * 100);
    }

    /// <summary>
    ///     Patch the <see cref="Terminal.SetItemSales" /> method to randomise moon travel prices.
    /// </summary>
    /// <param name="__instance"> The <see cref="Terminal" /> instance. </param>
    [HarmonyPatch("SetItemSales")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void RandomiseMoonTravelPrices(ref Terminal __instance)
    {
        var random = new Random(StartOfRound.Instance.randomMapSeed);

        var keywordsList = __instance.terminalNodes.allKeywords.ToList();

        var routeKeyword = keywordsList.Find(keyword => keyword.word == "route");

        var amountOfFreeMoons = OriginalMoonCosts.Values.Count > 0
            ? OriginalMoonCosts.Values.Count(moonCost => moonCost == 0)
            : routeKeyword.compatibleNouns.Count(compatibleNoun => compatibleNoun.result.itemCost == 0);

        // Loop through all route's compatible nouns.

        foreach (var compatibleNoun in routeKeyword.compatibleNouns)
        {
            // Skip the Company
            if (compatibleNoun.result.displayText.ToLower().Contains("company")) continue;

            // Add original moon cost to dictionary if key doesn't exist.
            // This ensures we can always revert back to the original moon cost.
            if (!OriginalMoonCosts.ContainsKey(compatibleNoun.noun.word))
                OriginalMoonCosts.Add(compatibleNoun.noun.word, compatibleNoun.result.itemCost);
            
            // Reset moon cost to original value.
            compatibleNoun.result.itemCost = OriginalMoonCosts[compatibleNoun.noun.word];

            // Ensures at least one originally free moon remains free.
            // Prevents campaign deadlocks.
            if (OriginalMoonCosts[compatibleNoun.noun.word] == 0 && amountOfFreeMoons > 0)
                if (random.Next(0, 2) == 0 || amountOfFreeMoons == 1)
                {
                    amountOfFreeMoons = -1;
                    continue;
                }
            
            var moonCost = OriginalMoonCosts[compatibleNoun.noun.word];

            // If moon cost is 0, randomly select whether to give it a random initial value to randomise.
            if (moonCost == 0)
            {
                if (random.Next(0, 5) == 0)
                    moonCost = random.Next(0, Max0MoonCost);
                else
                    continue;
            }

            // Randomise moon cost.
            moonCost = GetRandomisedMoonPrice(random, moonCost);

            compatibleNoun.result.itemCost = moonCost;

            // Set the price of the confirm option to the new moon cost.
            // Vanilla has the same price set for both the decision and confirm nodes.
            foreach (var compatibleNoun2 in compatibleNoun.result.terminalOptions)
                if (compatibleNoun2.noun.word.ToLower() == "confirm")
                    compatibleNoun2.result.itemCost = moonCost;
        }

        // Update original keywords.
        __instance.terminalNodes.allKeywords = keywordsList.ToArray();
    }

    private static int GetRandomisedMoonPrice(Random random, int originalPrice)
    {
        var buyingRateChange = random.Next(0, 2) switch
        {
            0 => random.NextDouble() * MaxMoonCostAddition,
            _ => random.NextDouble() * -MaxMoonCostSubtraction
        };

        return (int)(originalPrice * (1 + buyingRateChange));
    }
}