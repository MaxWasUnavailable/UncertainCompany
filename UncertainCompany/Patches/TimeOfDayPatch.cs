using System;
using HarmonyLib;

namespace UncertainCompany.Patches;

/// <summary>
///     Harmony patches for the <see cref="TimeOfDay" /> class.
/// </summary>
[HarmonyPatch(typeof(TimeOfDay))]
internal class TimeOfDayPatch
{
    private const double MaxBuyingRateAddition = 0.15;
    private const double MaxBuyingRateSubtraction = 0.20;

    /// <summary>
    ///     Patch the <see cref="TimeOfDay.SetBuyingRateForDay" /> method to randomise the buying rate.
    /// </summary>
    /// <param name="__instance"> The <see cref="TimeOfDay" /> instance. </param>
    [HarmonyPatch("SetBuyingRateForDay")]
    [HarmonyPostfix]
    internal static void RandomiseBuyingRate(ref TimeOfDay __instance)
    {
        var random = new Random(StartOfRound.Instance.randomMapSeed);

        var buyingRateChange = random.Next(0, 2) switch
        {
            0 => random.NextDouble() * MaxBuyingRateAddition,
            _ => random.NextDouble() * -MaxBuyingRateSubtraction
        };

        StartOfRound.Instance.companyBuyingRate += (float)buyingRateChange;
    }
}