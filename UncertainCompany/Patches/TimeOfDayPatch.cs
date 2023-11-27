using System;
using HarmonyLib;

namespace UncertainCompany.Patches;

/// <summary>
///     Harmony patches for the <see cref="TimeOfDay" /> class.
/// </summary>
[HarmonyPatch(typeof(TimeOfDay))]
internal class TimeOfDayPatch
{
    private const float MaxBuyingRateAddition = 0.15f;
    private const float MaxBuyingRateSubtraction = 0.20f;

    // ReSharper disable once InconsistentNaming
    private static readonly Random _random = new();

    /// <summary>
    ///     Patch the <see cref="TimeOfDay.SetBuyingRateForDay" /> method to randomise the buying rate.
    /// </summary>
    /// <param name="__instance"> The <see cref="TimeOfDay" /> instance. </param>
    [HarmonyPatch(nameof(TimeOfDay.SetBuyingRateForDay))]
    [HarmonyPostfix]
    internal static void RandomiseBuyingRate(ref TimeOfDay __instance)
    {
        var buyingRateChange = _random.NextDouble() switch
        {
            < 0.5 => _random.NextDouble() * MaxBuyingRateAddition,
            _ => _random.NextDouble() * MaxBuyingRateSubtraction * -1
        };

        StartOfRound.Instance.companyBuyingRate += (float)buyingRateChange;
    }
}