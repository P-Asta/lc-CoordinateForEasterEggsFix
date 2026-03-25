using HarmonyLib;

namespace Asta.CfeeLowVersionFix.Patches;

[HarmonyPatch(typeof(StunGrenadeItem), nameof(StunGrenadeItem.SetExplodeOnThrowClientRpc))]
internal static class StunGrenadeItemSetExplodeOnThrowClientRpcPatch
{
    private static void Postfix(StunGrenadeItem __instance)
    {
        if (__instance is null)
        {
            return;
        }

        bool explodeOnThrow = __instance.explodeOnThrow;
        if (!OriginalCfeeHooks.TryGenerateEggLine(__instance.transform.position, explodeOnThrow))
        {
            CfeeLowVersionFixPlugin.Log.LogDebug("Skipped local egg line generation because the original method could not be resolved.");
        }
    }
}
