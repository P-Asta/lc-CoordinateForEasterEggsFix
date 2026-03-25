using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Asta.CfeeLowVersionFix;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("kakeEdition.CFEE", BepInDependency.DependencyFlags.HardDependency)]
public sealed class CfeeLowVersionFixPlugin : BaseUnityPlugin
{
    internal const string PluginGuid = "asta.cfee.lowverfix";
    internal const string PluginName = "CFEE Low Version Fix";
    internal const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log = null!;
    internal static Harmony HarmonyInstance = null!;

    private void Awake()
    {
        Log = Logger;
        HarmonyInstance = new Harmony(PluginGuid);

        HarmonyInstance.PatchAll();
        OriginalCfeeHooks.Install(HarmonyInstance);

        Log.LogInfo($"{PluginName} loaded.");
    }

    private void Start()
    {
        // Run once more after all plugin Awake calls have finished so the bypass
        // still applies even if the original mod patched late during startup.
        OriginalCfeeHooks.Install(HarmonyInstance);
    }
}
