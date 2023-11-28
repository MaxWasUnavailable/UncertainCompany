using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace UncertainCompany;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class UncertainCompany : BaseUnityPlugin
{
    private bool _isPatched;
    private Harmony Harmony { get; set; }
    private new static ManualLogSource Logger { get; set; }
    public static UncertainCompany Instance { get; private set; }
    public ConfigEntry<bool> DoHideWeather { get; private set; }
    public ConfigEntry<bool> DoRandomiseItemSales { get; private set; }
    public ConfigEntry<bool> DoRandomiseSellPrice { get; private set; }
    public ConfigEntry<bool> DoRandomiseTravelCost { get; private set; }

    private void Awake()
    {
        // Set instance
        Instance = this;

        // Init logger
        Logger = base.Logger;
        
        // Init config entries
        DoHideWeather = Config.Bind("General", "HideWeather", true, "Hide weather information.");
        DoRandomiseItemSales = Config.Bind("General", "RandomiseItemSales", true, "Randomise item sales. (Can be cheaper *or* more expensive)");
        DoRandomiseSellPrice = Config.Bind("General", "RandomiseSellPrice", true, "Randomise sell price. (Can sell for more *or* less)");
        DoRandomiseTravelCost = Config.Bind("General", "RandomiseTravelCost", true, "Randomise travel cost. (Can also impact free moons)");

        // Patch using Harmony
        PatchAll();

        // Report plugin is loaded
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    public void PatchAll()
    {
        if (_isPatched)
        {
            Logger.LogWarning("Already patched!");
            return;
        }

        Logger.LogDebug("Patching...");

        Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();
        _isPatched = true;

        Logger.LogDebug("Patched!");
    }

    public void UnpatchAll()
    {
        if (!_isPatched)
        {
            Logger.LogWarning("Already unpatched!");
            return;
        }

        Logger.LogDebug("Unpatching...");

        Harmony.UnpatchSelf();
        _isPatched = false;

        Logger.LogDebug("Unpatched!");
    }
}