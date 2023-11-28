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
    private static Random _random;

    /// <summary>
    ///     Patch the <see cref="TimeOfDay.SetBuyingRateForDay" /> method to randomise the buying rate.
    /// </summary>
    /// <param name="__instance"> The <see cref="TimeOfDay" /> instance. </param>
    [HarmonyPatch("SetBuyingRateForDay")]
    [HarmonyPostfix]
    internal static void RandomiseBuyingRate(ref TimeOfDay __instance)
    {
        if (_random == null)
        {
            _random = new Random(StartOfRound.Instance.randomMapSeed);
        }
        
        var buyingRateChange = _random.NextDouble() switch
        {
            < 0.5 => _random.NextDouble() * MaxBuyingRateAddition,
            _ => _random.NextDouble() * MaxBuyingRateSubtraction * -1
        };

        buyingRateChange *= 100;

        StartOfRound.Instance.companyBuyingRate += (float) buyingRateChange;
    }
}