using HarmonyLib;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Utility;

namespace CombatLogDCBreakdown;

/// <summary>
/// Mod localization
/// </summary>
[HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.LoadPack), typeof(Locale))]
internal static class ModLocalization
{
    private readonly static Dictionary<string, string> translations = new()
    {
            {"enGB", "Difficulty (DC):"},
            {"deDE", "Schwierigkeitsgrad (SG):"},
            {"esES", "Dificultad (CD):"},
            {"frFR", "Difficulté (DD) :"},
            {"itIT", "Difficoltà (CD):"},
            {"ptBR", "Dificuldade (CD):"},
            {"ruRU", "Сложность:"},
            {"zhCN", "难度（DC）："},
    };

    private readonly static Dictionary<string, string> conditionalBonusTranslations = new()
    {
            {"enGB", "Conditional bonus:"},
            {"deDE", "Bedingter Bonus:"},
            {"esES", "Bonificación condicional:"},
            {"frFR", "Bonus conditionnel :"},
            {"itIT", "Bonus condizionale:"},
            {"ptBR", "Bônus condicional:"},
            {"ruRU", "Условный бонус:"},
            {"zhCN", "条件加成："},
    };

    private readonly static Dictionary<string, string> mythicRankTranslations = new()
    {
            {"enGB", "Mythic rank"},
            {"deDE", "Legendenrang"},
            {"esES", "Rango mítico"},
            {"frFR", "Rang mythique"},
            {"itIT", "Rango Mitico"},
            {"ptBR", "Nível mítico"},
            {"ruRU", "Мифический уровень"},
            {"zhCN", "神话阶层"},
    };

    public static LocalizedString DCString = new() { m_Key = "293aa85a-cc72-4e40-a658-6961ff23dc23" };
    public static LocalizedString ConditionalBonusString = new() { m_Key = "390be091-baf6-43da-a827-a0ee97e21a13" };
    public static LocalizedString MythicRankString = new() { m_Key = "4f6d0020-6f0c-442d-805a-ad9de7d5a4b9" };

    [HarmonyPostfix]
    public static void Init(LocalizationPack __result, Locale locale)
    {
        var name = translations.Get(locale.ToString()) ?? translations["enGB"];
        __result.PutString(DCString.m_Key, name);
        var conditionalBonusName = conditionalBonusTranslations.Get(locale.ToString()) ?? conditionalBonusTranslations["enGB"];
        __result.PutString(ConditionalBonusString.m_Key, conditionalBonusName);
        var mythicRankName = mythicRankTranslations.Get(locale.ToString()) ?? mythicRankTranslations["enGB"];
        __result.PutString(MythicRankString.m_Key, mythicRankName);
    }
}
