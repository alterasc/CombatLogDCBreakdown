using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Localization;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using System.Reflection.Emit;
using System.Text;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches SavingThrowMessage which creates saving throw message log
/// to include DC breakdown information in the log message
/// </summary>
[HarmonyPatch(typeof(SavingThrowMessage), nameof(SavingThrowMessage.GetData))]
public static class SavingThrowMessagePatcher
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions)
            .MatchEndForward(
                CodeMatch.LoadsField(AccessTools.Field(typeof(SavingThrowMessage), nameof(SavingThrowMessage.Tooltip))),
                CodeMatch.Calls(AccessTools.Method(typeof(LocalizedString), "op_Implicit")),
                CodeMatch.Calls(AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.Append), [typeof(string)]))
            )
            .InsertAfter(
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(SavingThrowMessagePatcher), nameof(AppendModifiersBreakdownExtended))
            );
        return matcher.Instructions();
    }

    private static readonly LocalizedString spellLevel = new() { m_Key = "6221aa8e-5d21-44d8-9c1f-921e081c4ae3" };

    private static readonly LocalizedString difficulty = new() { m_Key = "a3a90870-c80e-4b0e-842f-15a70493b202" };
    public static StringBuilder AppendModifiersBreakdownExtended(StringBuilder builder, RuleSavingThrow rule)
    {
        if (BreakdownStorage.RuleSavingThrowTable.TryGetValue(rule, out var breakdown))
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine($"<b>{ModLocalization.DCString} {rule.DifficultyClass}</b>");
            if (breakdown.BaseDC != 0)
            {
                builder.AppendLine($"{UIStrings.Instance.Tooltips.BaseValue}: {breakdown.BaseDC}");
            }
            if (breakdown.SpellLevel > 0)
            {
                builder.Append($"{spellLevel}: ");
                AppendStat(builder, breakdown.SpellLevel);
            }
            if (breakdown.StatBonus != 0)
            {
                builder.Append($"{UIUtility.GetStatText(breakdown.StatBonusSource)}: ");
                AppendStat(builder, breakdown.StatBonus);
            }
            IEnumerable<Modifier> allBonuses = breakdown.IgnoreDCBonuses ? [] : breakdown.BonusDC?.Modifiers ?? [];
            if (breakdown.SecondaryBonusDC != null)
            {
                allBonuses = allBonuses.Concat(breakdown.SecondaryBonusDC?.Modifiers ?? []);
            }
            foreach (var modifier in allBonuses)
            {
                try
                {
                    string name = StatModifiersBreakdown.GetBonusSourceText((IUIDataProvider)modifier.Fact?.SourceItem ?? modifier.Fact, true);
                    if (string.IsNullOrEmpty(name) && modifier.Fact.Blueprint != null)
                    {
                        if (NameOverrides.Overrides.TryGetValue(modifier.Fact.Blueprint.AssetGuid.m_Guid, out var overrideBlueprint)
                            && overrideBlueprint.Get() is IUIDataProvider uIDataProvider)
                        {
                            name = uIDataProvider.Name;
                        }
                        else
                        {
                            name = modifier.Fact.Blueprint.name;
                        }
                    }
                    StatModifiersBreakdown.AppendBonus(builder, modifier.Value, name, modifier.Descriptor, null);
                }
                catch (Exception)
                {
                    builder.AppendLine($"error: {UIUtility.AddSign(modifier.Value)}");
                }
            }
            if (breakdown.EnemyDifficultyBonus != 0)
            {
                builder.Append($"{difficulty}: ");
                AppendStat(builder, breakdown.EnemyDifficultyBonus);
            }
            if (breakdown.EnemyDCCap != 0)
            {
                builder.Append($"Limited by difficulty to: ");
                AppendStat(builder, breakdown.EnemyDCCap);
            }
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
