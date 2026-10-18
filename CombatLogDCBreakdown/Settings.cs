using UnityEngine;
using UnityModManagerNet;

namespace CombatLogDCBreakdown;

public class Settings : UnityModManager.ModSettings
{
    public bool FixStatBonusInSavingThrowBreakdown = true;

    private static GUIStyle tttWarningStyle;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        if (Main.IsTTTEnabled)
        {
            GUI.enabled = false;
        }
        Main.Settings.FixStatBonusInSavingThrowBreakdown = GUILayout.Toggle(
            Main.Settings.FixStatBonusInSavingThrowBreakdown,
            " Fix stat bonus missing in saving throw breakdown");
        GUI.enabled = true;
        GUILayout.Label("");
        GUILayout.Label("Change in settings applies only after game restart.");
        if (Main.IsTTTEnabled)
        {
            tttWarningStyle ??= new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.red },
                wordWrap = true,
            };
            GUILayout.Label(
                "TabletopTweaks-Base is installed and similar fix is present there. This mod's fix is not applied.",
                tttWarningStyle);
        }
    }
}
