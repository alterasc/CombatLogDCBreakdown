using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace CombatLogDCBreakdown.SavingThrowBreakdownFix;

[HarmonyPatch(typeof(RuleSavingThrow), nameof(RuleSavingThrow.SetStatValueFromThrow))]
[HarmonyAfter("TabletopTweaks-Base")]
static class RuleSavingThrow_SetStatValueFromThrow_Patch
{
    internal class CustomModifier
    {
        internal int value;
        internal StatType statType;

    }
    public static ConditionalWeakTable<RuleSavingThrow, CustomModifier> RuleSavingThrowStatBonusTable = new();

    [HarmonyPrepare]
    static bool MyInitializer(MethodBase original)
    {
        if (Main.IsTTTEnabled)
        {
            Main.log.Log($"TabletopTweaks-Base is enabled, skipping patch RuleSavingThrow_SetStatValueFromThrow_Patch");
            return false;
        }
        if (!Main.Settings.FixStatBonusInSavingThrowBreakdown)
        {
            Main.log.Log($"Setting is disabled, skipping patch RuleSavingThrow_SetStatValueFromThrow_Patch");
            return false;
        }
        return true;
    }

    static void Postfix(RuleSavingThrow __instance, ModifiableValue stat)
    {
        if (!Main.Settings.FixStatBonusInSavingThrowBreakdown)
        {
            return;
        }
        if (stat is ModifiableValueSavingThrow savingThrow)
        {
            using (savingThrow.GetTemporaryModifiersScope(__instance.AllBonuses))
            {
                if (savingThrow.BaseStat.Bonus != 0)
                {
                    RuleSavingThrowStatBonusTable.Add(__instance, new CustomModifier() { value = savingThrow.BaseStat.Bonus, statType = savingThrow.BaseStat.Type });
                }
            }
        }
    }
}


[HarmonyPatch(typeof(SavingThrowMessage), nameof(SavingThrowMessage.GetData))]
[HarmonyAfter("TabletopTweaks-Base")]
public static class SavingThrowStatPatcher
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        if (Main.IsTTTEnabled)
        {
            Main.log.Log($"TabletopTweaks-Base is enabled, skipping patch SavingThrowStatPatcher");
            return instructions;
        }
        if (!Main.Settings.FixStatBonusInSavingThrowBreakdown)
        {
            Main.log.Log($"Setting is disabled, skipping patch SavingThrowStatPatcher");
            return instructions;
        }

        var codes = new List<CodeInstruction>(instructions);
        var appendMethod = AccessTools.Method(typeof(StatModifiersBreakdown), nameof(StatModifiersBreakdown.AppendModifiersBreakdown));
        for (int i = 0; i < codes.Count - 2; i++)
        {
            if (codes[i].opcode != OpCodes.Ldloc_S)
                continue;
            if (codes[i + 1].opcode != OpCodes.Ldstr)
                continue;
            if (!codes[i + 2].Calls(appendMethod))
                continue;

            codes.InsertRange(i,
            [
                new CodeInstruction(codes[i]),
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(SavingThrowStatPatcher), nameof(AppendModifiersBreakdownExtended))
            ]);
            Main.log.Log($"SavingThrowStatPatcher: Patched {__originalMethod.ShortDescription()}");
            return codes.AsEnumerable();
        }
        Helpers.ReportTranspilerFailure(__originalMethod);
        return codes.AsEnumerable();
    }

    public static StringBuilder AppendModifiersBreakdownExtended(StringBuilder builder, RuleSavingThrow rule)
    {
        if (RuleSavingThrow_SetStatValueFromThrow_Patch.RuleSavingThrowStatBonusTable.TryGetValue(rule, out var breakdown))
        {
            builder.Append($"{UIUtility.GetStatText(breakdown.statType)}: ");
            AppendStat(builder, breakdown.value);
        }
        return builder;
    }

    private static void AppendStat(StringBuilder sb, int bonusValue)
    {
        string value = (bonusValue < 0) ? StatModifiersBreakdown.PenaltyColor : StatModifiersBreakdown.BonusColor;
        sb.Append("<color=#").Append(value).Append('>');
        sb.Append(UIUtility.AddSign(new int?(bonusValue)));
        sb.Append("</color>");
        sb.AppendLine();
    }
}