using GameData;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.ModulesAPI;

namespace Hikaria.DamageAnalyzer.Managers;

internal static class TranslateManager
{
    public static string KILL => Features.DamageAnalyzer.Localization.Get(1);
    public static string IMMORTAL => Features.DamageAnalyzer.Localization.Get(2);
    public static string UNKNOWN => Features.DamageAnalyzer.Localization.Get(3);
    public static string ARMOR => Features.DamageAnalyzer.Localization.Get(4);
    public static string CRIT => Features.DamageAnalyzer.Localization.Get(5);
    public static string BACKMULTI => Features.DamageAnalyzer.Localization.Get(6);
    public static string SLEEPMULTI => Features.DamageAnalyzer.Localization.Get(7);
    public static string HITPOSITION_DESC => Features.DamageAnalyzer.Localization.Get(8);

    public static string EnemyName(uint id)
    {
        if (!EnemyID2NameLookup.TryGetValue(id, out string result))
        {
            result = $"{EnemyDataBlock.GetBlock(id).name} [{id}]";
        }
        return result;
    }

    public static string EnemyLimb(string LimbName)
    {
        LimbName = LimbName.ToLower();
        if (EnemyLimbNamesLookup.Any((x) =>
        {
            if (LimbName.Contains(x.Key))
            {
                LimbName = x.Value;
                return true;
            }
            return false;
        }))
        {
            return LimbName;
        }
        return UNKNOWN;
    }

    private static CustomSetting<List<EnemyIDName>> EnemyIDNameSettings = new("EnemyIDNames", new(), (data) =>
    {
        EnemyID2NameLookup = new();
        foreach (var item in data)
        {
            foreach (var id in item.IDs)
            {
                EnemyID2NameLookup[id] = item.Name;
            }
        }
    });
    private static CustomSetting<List<EnemyLimbName>> EnemyLimbNameSettings = new("EnemyLimbNames", new(), (data) =>
    {
        EnemyLimbNamesLookup = data.ToDictionary(x => x.Contain.ToLower(), x => x.Name);
    });
    private static Dictionary<string, string> EnemyLimbNamesLookup;
    private static Dictionary<uint, string> EnemyID2NameLookup;

    public struct EnemyIDName
    {
        public List<uint> IDs;

        public string Name;
    }

    private struct EnemyLimbName
    {
        public string Contain;

        public string Name;
    }
}
