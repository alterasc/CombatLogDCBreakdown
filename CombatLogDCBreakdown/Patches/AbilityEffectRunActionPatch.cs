using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches creation of RuleSavingThrow in AbilityEffectRunAction 
/// to use a custom method that passes breakdown information
/// </summary>
[HarmonyPatch(typeof(AbilityEffectRunAction), nameof(AbilityEffectRunAction.CreateSavingThrow))]
internal static class AbilityEffectRunActionPatch
{
    [HarmonyPostfix]
    public static void CreateSavingThrowExtended(AbilityEffectRunAction __instance, UnitEntityData unit, AbilityExecutionContext context, bool persistentSpell, RuleSavingThrow __result)
    {
        if (BreakdownStorage.BreakdownTable.TryGetValue(context.Params, out var breakdown))
        {
            BreakdownStorage.RuleSavingThrowTable.Add(__result, breakdown);
        }
    }
}
