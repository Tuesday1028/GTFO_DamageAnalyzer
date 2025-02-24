using Agents;
using Hikaria.Core;
using Hikaria.Core.SNetworkExt;
using Hikaria.DamageAnalyzer.Handlers;
using SNetwork;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hikaria.DamageAnalyzer.Managers;

public static class DamageInfoManager
{
    public static void Setup()
    {
        m_packet = SNetExt_AuthorativeAction<pDamageInfo>.Create(typeof(pDamageInfo).FullName, ReceiveDamageInfo, null, p => CoreAPI.IsPlayerInstalledMod(p, PluginInfo.GUID), SNet_ChannelType.GameNonCritical);
    }

    public static void SendDamageInfo(BasicDamageInfo damageInfo, SNet_Player player)
    {
        m_packet.Ask(new(player, damageInfo));
    }

    private static void ReceiveDamageInfo(ulong senderID, pDamageInfo data)
    {
        if (data.player.TryGetPlayer(out var player) && player.IsLocal && SNet.TryGetPlayer(senderID, out var sender) && sender.IsMaster)
        {
            OriginalBulletDamage[player.Lookup] = data.damageInfo.rawDamage;
            ProcessDamageInfo(data.damageInfo);
        }
    }

    public static void ProcessDamageInfo(BasicDamageInfo damageInfo)
    {
        if (!damageInfo.player.TryGetPlayer(out var player) || !damageInfo.enemy.TryGet(out var enemy))
            return;

        var limb = enemy.Damage.DamageLimbs[damageInfo.limbID];
        float realDamage = damageInfo.rawDamage;
        bool isCrit = false;
        bool isArmor = false;
        bool isBackMulti = false;
        bool isSleepMulti = damageInfo.isSleepMulti;
        bool isImmortal = enemy.Damage.IsImortal;
        bool allowDirectionalBonus = damageInfo.allowDirectionalBonus;
        float PrecisionMulti = damageInfo.precisionMulti;
        float SleeperMulti = damageInfo.sleeperMulti;
        float BackStubberMulti = damageInfo.backstabberMulti;
        Vector3 Direction = damageInfo.direction;
        Vector3 Position = damageInfo.position;
        DamageType damageType = damageInfo.damageType;
        DamageSource damageSource = damageInfo.damageSource;
        if (limb != null)
        {
            if (limb.m_type == eLimbDamageType.Weakspot)
            {
                isCrit = true;
            }
            else if (limb.m_type == eLimbDamageType.Armor)
            {
                isArmor = true;
            }
        }

        if (damageType == DamageType.Bullet)
        {
            if (damageSource == DamageSource.SentryGun)
            {
                if (!player.IsLocal)
                {
                    if (SNet.IsMaster && m_packet.Listeners.Any(p => p.Lookup == player.Lookup))
                    {
                        SendDamageInfo(damageInfo, player);
                    }
                    return;
                }
                if (player.IsLocal && !ShowSentryDamage)
                {
                    return;
                }
            }
            realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.ProjectileResistance, OriginalBulletDamage[player.Lookup]); // 敌人的炮弹抗性
        }
        else
        {
            realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.MeleeResistance, realDamage); // 敌人的近战抗性
            if (isSleepMulti)
            {
                realDamage *= SleeperMulti;
            }
        }

        realDamage = limb.ApplyWeakspotAndArmorModifiers(realDamage, PrecisionMulti); //算上特殊部位伤害倍率

        if (allowDirectionalBonus)
        {
            float beforeBackMultiDamage = realDamage;
            realDamage = limb.ApplyDamageFromBehindBonus(realDamage, Position, Direction, BackStubberMulti); // 算上后背加成
            if (realDamage > beforeBackMultiDamage) // 后背加成后伤害更高了则有后背加成
            {
                isBackMulti = true;
            }
        }

        DamageInfoUpdater.Instance.UpdateBasicDamageInfo(limb, isImmortal, isArmor, isCrit, isBackMulti, IsSleepMulti, realDamage);
    }

    public static bool ShowSentryDamage = true;
    public static uint LocalAgentGlobalID;
    public static bool IsSleepMulti;
    public static bool IsSentryGunFire;
    public static Dictionary<ulong, float> OriginalBulletDamage = new();
    private static SNetExt_AuthorativeAction<pDamageInfo> m_packet;

    public struct pDamageInfo
    {
        public pDamageInfo(SNet_Player player, BasicDamageInfo damageInfo)
        {
            this.player = new();
            this.player.SetPlayer(player);
            this.damageInfo = damageInfo;
        }

        public SNetStructs.pPlayer player;

        public BasicDamageInfo damageInfo;
    }

    public struct BasicDamageInfo
    {
        public BasicDamageInfo(SNet_Player player, DamageType damageType, DamageSource damageSource, Dam_EnemyDamageLimb limb, Vector3 position, Vector3 direction, float rawDamage, bool allowDirectionalBonus = false, float staggerMulti = 1f, float precisionMulti = 1f, float backstabberMulti = 1f, float sleeperMulti = 1f, bool isSleepMulti = false)
        {
            this.player = new();
            this.player.SetPlayer(player);
            this.damageType = damageType;
            this.damageSource = damageSource;
            enemy = new();
            enemy.Set(limb.m_base.Owner);
            limbID = limb.m_limbID;
            this.position = position;
            this.direction = direction;
            this.rawDamage = rawDamage;
            this.allowDirectionalBonus = allowDirectionalBonus;
            this.staggerMulti = staggerMulti;
            this.precisionMulti = precisionMulti;
            this.backstabberMulti = backstabberMulti;
            this.sleeperMulti = sleeperMulti;
            this.isSleepMulti = isSleepMulti;
        }

        public SNetStructs.pPlayer player;

        public pEnemyAgent enemy;

        public float rawDamage;

        public int limbID;

        public bool allowDirectionalBonus;

        public bool isSleepMulti;

        public float staggerMulti;

        public float precisionMulti;

        public float backstabberMulti;

        public float sleeperMulti;

        public DamageType damageType;

        public DamageSource damageSource;

        public Vector3 position;

        public Vector3 direction;
    }

    public enum DamageType : byte
    {
        Bullet,
        Melee
    }

    public enum DamageSource : byte
    {
        SentryGun,
        Player
    }
}
