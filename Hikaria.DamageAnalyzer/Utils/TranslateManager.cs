using GameData;
using Hikaria.DamageAnalyzer.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hikaria.DamageAnalyzer.Utils
{
    internal static class TranslateManager
    {
        public static string EnemyName(uint id)
        {
            if (!EnemyID2Name.TryGetValue(id, out string Name))
            {
                StringBuilder sb = new StringBuilder(100);
                sb.Append(EnemyDataBlock.GetBlock(id).name);
                sb.Append($" [{id}]");
                Name = sb.ToString();
            }
            return Name;
        }

        public static string EnemyLimb(string LimbName)
        {
            LimbName = LimbName.ToLower();
            if (EnemyLimbNamesDict.Any((x) =>
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
            return DamageDisplay.Instance.language.UNKNOWN;
        }

        public static void Setup()
        {
            JsonHelper helper = new();

            EnemyLimbNamesDict.Clear();
            helper.TryRead(EnemyLimbNameFile, out List<EnemyLimbNames> enemyLimbNames);
            EnemyLimbNamesDict = enemyLimbNames.ToDictionary(x => x.Contain.ToLower(), x => x.Name);

            EnemyID2Name.Clear();
            helper.TryRead(EnemyIDNameFile, out List<EnemyIDName> enemyIDNames);
            foreach (var item in enemyIDNames)
            {
                foreach (var id in item.IDs)
                {
                    EnemyID2Name.Add(id, item.Name);
                }
            }
        }

        private static Dictionary<string, string> EnemyLimbNamesDict = new();

        private static Dictionary<uint, string> EnemyID2Name = new();

        [Serializable]
        public struct EnemyIDName
        {
            public List<uint> IDs;

            public string Name;
        }

        [Serializable]
        private struct EnemyLimbNames
        {
            public string Contain;

            public string Name;
        }

        private static readonly string EnemyLimbNameFile = string.Concat(BepInEx.Paths.ConfigPath, "\\Hikaria\\DamageAnalyzer\\EnemyLimbNames.json");

        private static readonly string EnemyIDNameFile = string.Concat(BepInEx.Paths.ConfigPath, "\\Hikaria\\DamageAnalyzer\\EnemyIDNames.json");
    }
}
