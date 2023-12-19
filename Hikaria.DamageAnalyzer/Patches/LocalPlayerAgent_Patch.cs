using HarmonyLib;
using Hikaria.DamageAnalyzer.Handlers;
using Player;
using UnityEngine;

namespace Hikaria.DamageAnalyzer.Patches
{
    [HarmonyPatch]
    public static class LocalPlayerAgent_Patch
    {
        [HarmonyPatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        [HarmonyPostfix]
        public static void LocalPlayerAgent__Setup__Postfix(LocalPlayerAgent __instance)
        {
            GameObject gameObject = __instance.gameObject;
            if (gameObject.GetComponent<DamageDisplay>() == null)
            {
                gameObject.AddComponent<DamageDisplay>();
            }
        }
    }
}
