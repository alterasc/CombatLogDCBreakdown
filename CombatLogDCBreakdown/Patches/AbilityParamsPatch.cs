using HarmonyLib;
using Kingmaker.UnitLogic.Abilities;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches AbilityParams.Clone to copy breakdown information to the cloned instance
/// </summary>
[HarmonyPatch]
public static class AbilityParamsPatch
{
    [HarmonyPatch(typeof(AbilityParams), nameof(AbilityParams.Clone))]
    [HarmonyPostfix]
    public static void CopyOnClone(AbilityParams __instance, AbilityParams __result)
    {
        if (BreakdownStorage.BreakdownTable.TryGetValue(__instance, out var breakdown))
        {
            BreakdownStorage.BreakdownTable.Add(__result, breakdown);
        }
    }
}
