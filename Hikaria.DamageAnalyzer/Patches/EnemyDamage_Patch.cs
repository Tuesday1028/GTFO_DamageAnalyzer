using Agents;
using Enemies;
using Gear;
using HarmonyLib;
using Player;
using SNetwork;
using System.Collections.Generic;
using UnityEngine;
using static Hikaria.DamageAnalyzer.Managers.DamageInfoManager;

namespace Hikaria.DamageAnalyzer.Patches;

[HarmonyPatch]
public static class EnemyDamage_Patch
{
    [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
    [HarmonyPrefix]
    private static void BulletWeapon__BulletHit__Prefix(Weapon.WeaponHitData weaponRayData, bool doDamage, float additionalDis)
    {
        DoBulletDamage = doDamage;
        SNet_Player player = weaponRayData.owner.Owner;
        if (!OriginalBulletDamage.TryAdd(player.Lookup, weaponRayData.damage))
        {
            OriginalBulletDamage[player.Lookup] = weaponRayData.damage;
        }
        float realDistance = weaponRayData.rayHit.distance + additionalDis;
        if (realDistance > weaponRayData.damageFalloff.x)
        {
            OriginalBulletDamage[player.Lookup] *= Mathf.Max(1f - (realDistance - weaponRayData.damageFalloff.x) / (weaponRayData.damageFalloff.y - weaponRayData.damageFalloff.x), BulletWeapon.s_falloffMin);
        }
    }

    [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
    [HarmonyPrefix]
    private static void Dam_EnemyDamageLimb__MeleeDamage__Prefix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent, Vector3 position, Vector3 direction, float staggerMulti = 1f, float precisionMulti = 1f, float backstabberMulti = 1f, float sleeperMulti = 1f)
    {
        if (sourceAgent == null || LocalAgentGlobalID != sourceAgent.GlobalID)
        {
            return;
        }
        PlayerAgent sourcePlayer = sourceAgent.TryCast<PlayerAgent>();
        if (sourcePlayer.Owner.IsBot)
        {
            return;
        }
        if (__instance.m_base.Owner.Locomotion.CurrentStateEnum == ES_StateEnum.Hibernate)
        {
            IsSleepMulti = true;
        }
    }


    [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
    [HarmonyPostfix]
    private static void Dam_EnemyDamageLimb__MeleeDamage__Postfix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent, Vector3 position, Vector3 direction, float staggerMulti = 1f, float precisionMulti = 1f, float backstabberMulti = 1f, float sleeperMulti = 1f)
    {
        if (sourceAgent == null || LocalAgentGlobalID != sourceAgent.GlobalID)
        {
            return;
        }
        PlayerAgent sourcePlayer = sourceAgent.TryCast<PlayerAgent>();
        if (sourcePlayer.Owner.IsBot)
        {
            return;
        }
        ProcessDamageInfo(new BasicDamageInfo(sourcePlayer.Owner, DamageType.Melee, DamageSource.Player, __instance, position, direction, dam, true, staggerMulti, precisionMulti, backstabberMulti, sleeperMulti, IsSleepMulti));
        IsSleepMulti = false;
    }

    [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
    [HarmonyPostfix]
    private static void Dam_EnemyDamageLimb__BulletDamage__Postfix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent, Vector3 position, Vector3 direction, bool allowDirectionalBonus, float staggerMulti = 1f, float precisionMulti = 1f)
    {
        if (sourceAgent == null || (!SNet.IsMaster && LocalAgentGlobalID != sourceAgent.GlobalID))
        {
            return;
        }
        PlayerAgent sourcePlayer = sourceAgent.TryCast<PlayerAgent>();
        if (sourcePlayer.Owner.IsBot)
        {
            return;
        }
        ProcessDamageInfo(new BasicDamageInfo(sourcePlayer.Owner, DamageType.Bullet, IsSentryGunFire ? DamageSource.SentryGun : DamageSource.Player, __instance, position, direction, dam, allowDirectionalBonus, staggerMulti, precisionMulti));
    }

    [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.FireBullet))]
    [HarmonyPrefix]
    private static void SentryGunInstance_Firing_Bullets__FireBullet__Prefix(SentryGunInstance_Firing_Bullets __instance)
    {
        IsSentryGunFire = true;
    }

    [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.FireBullet))]
    [HarmonyPostfix]
    private static void SentryGunInstance_Firing_Bullets__FireBullet__Postfix()
    {
        IsSentryGunFire = false;
    }

    [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireShotgunSemi))]
    [HarmonyPrefix]
    private static void SentryGunInstance_Firing_Bullets__UpdateFireShotgunSemi__Prefix(SentryGunInstance_Firing_Bullets __instance)
    {
        IsSentryGunFire = true;
    }

    [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireShotgunSemi))]
    [HarmonyPostfix]
    private static void SentryGunInstance_Firing_Bullets__UpdateFireShotgunSemi__Postfix()
    {
        IsSentryGunFire = false;
    }

    [HarmonyPatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
    [HarmonyPostfix]
    private static void LocalPlayerAgent__Setup__Postfix(LocalPlayerAgent __instance)
    {
        LocalAgentGlobalID = __instance.GlobalID;
    }

    public static uint LocalAgentGlobalID { get; private set; }

    public static bool IsSleepMulti { get; private set; }

    public static bool IsSentryGunFire { get; private set; }

    public static Dictionary<ulong, float> OriginalBulletDamage { get; private set; } = new();

    public static bool DoBulletDamage { get; private set; }
}
