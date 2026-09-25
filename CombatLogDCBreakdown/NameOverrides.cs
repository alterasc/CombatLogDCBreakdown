using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;

namespace CombatLogDCBreakdown;

/// <summary>
/// Dictionary of overrides for blueprints that have no name and where
/// SourceFact and SourceItem do not return sensible results to display in log.
/// For those blueprints, source will be replaced with the name of the blueprint referenced in the dictionary.
/// </summary>
public static class NameOverrides
{
    public static Dictionary<Guid, BlueprintReference<BlueprintFact>> Overrides = new()
    {
        { Guid.Parse("ea7c96bad79347318961e12a6642de66"), new() { deserializedGuid = BlueprintGuid.Parse("1c28fa1d710041289dd83e18dfc8316d") } }, //Cloak of Morta
    };
}
