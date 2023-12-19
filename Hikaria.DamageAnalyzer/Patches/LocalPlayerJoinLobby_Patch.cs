using HarmonyLib;
using Hikaria.DamageAnalyzer.Managers;
using SNetwork;

namespace Hikaria.DamageAnalyzer.Patches
{
    [HarmonyPatch]
    public static class LocalPlayerJoinLobby_Patch
    {
        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnPlayerSpawnedAgent))]
        [HarmonyPostfix]
        private static void SNet_SyncManager__OnPlayerSpawnedAgent__Postfix(SNet_Player player)
        {
            if (player == null || player.PlayerSlotIndex() < 0)
            {
                return;
            }
            if (player.IsLocal && !SNet.IsMaster)
            {
                DamageInfoManager.RequestRegistDamageInfoReceiver();
            }
        }

        [HarmonyPatch(typeof(GS_Lobby), nameof(GS_Lobby.OnMasterChanged))]
        [HarmonyPostfix]
        private static void GS_Lobby__OnMasterChanged__Postfix()
        {
            if (!SNet.IsMaster)
            {
                DamageInfoManager.RequestRegistDamageInfoReceiver();
            }
        }
    }
}
