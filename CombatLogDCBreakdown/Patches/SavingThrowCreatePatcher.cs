using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System.Reflection;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches the calls to ContextActionSavingThrow.CreateSavingThrow to use a custom method that passes breakdown information 
/// from AbilityParams to RuleSavingThrow
/// </summary>
[HarmonyPatch(typeof(ContextActionSavingThrow), nameof(ContextActionSavingThrow.RunAction))]
internal static class SavingThrowCreatePatcher
{
    private static readonly MethodInfo _method = AccessTools.Method(typeof(ContextActionSavingThrow), nameof(ContextActionSavingThrow.CreateSavingThrow));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (!instruction.Calls(_method))
            {
                yield return instruction;
            }
            else
            {
                yield return CodeInstruction.LoadLocal(0); // The only local in this method is local that holds conditional bonus
                yield return CodeInstruction.Call(typeof(SavingThrowCreatePatcher), nameof(CreateSavingThrowExtended));
                Main.log.Log("Replaced CreateSavingThrow call!");
            }
        }
    }

    public static RuleSavingThrow CreateSavingThrowExtended(ContextActionSavingThrow action, UnitEntityData unit, int dc, bool persistentSpell, int conditionalBonus)
    {
        var result = action.CreateSavingThrow(unit, dc, persistentSpell);
        if (BreakdownStorage.BreakdownTable.TryGetValue(action.Context.Params, out var breakdown))
        {
            BreakdownStorage.RuleSavingThrowTable.Add(result, breakdown);
        }
        if (conditionalBonus != 0)
        {
            var conditionalDCBonus = BreakdownStorage.RuleSavingThrowConditionalBonusTable.GetOrCreateValue(result);
            conditionalDCBonus.Value = conditionalBonus;
        }
        return result;
    }
}
