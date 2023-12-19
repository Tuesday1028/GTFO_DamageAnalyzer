using Agents;
using Enemies;
using GTFO.API;
using Hikaria.DamageAnalyzer.Config;
using Hikaria.DamageAnalyzer.Handlers;
using SNetwork;
using System.Collections.Generic;
using UnityEngine;
using static Hikaria.DamageAnalyzer.Patches.EnemyDamage_Patch;

namespace Hikaria.DamageAnalyzer.Managers
{
    public static class DamageInfoManager
    {
        public static void Init()
        {
            ShowSentryDamage = ConfigManager.ShowSentryDamage.Value;
            NetworkAPI.RegisterEvent<pDamageInfo>(typeof(pDamageInfo).FullName, ReceiveDamageInfo);
            NetworkAPI.RegisterEvent<pRegistDamageInfoReceiver>(typeof(pRegistDamageInfoReceiver).FullName, ReceiveRequestRegistDamageInfoReceiver);
        }

        public static void SendDamageInfo(BasicDamageInfo damageInfo, SNet_Player player)
        {
            pDamageInfo data = new(player, damageInfo);
            NetworkAPI.InvokeEvent(typeof(pDamageInfo).FullName, data, player, SNet_ChannelType.GameNonCritical);
        }

        private static void ReceiveDamageInfo(ulong senderID, pDamageInfo data)
        {
            if (data.player.TryGetPlayer(out var player) && player.IsLocal && SNet.TryGetPlayer(senderID, out var sender) && sender.IsMaster)
            {
                OriginalBulletDamage[player.Lookup] = data.damageInfo._rawDamage;
                ProcessDamageInfo(data.damageInfo);
            }
        }

        private static void ReceiveRequestRegistDamageInfoReceiver(ulong senderID, pRegistDamageInfoReceiver data)
        {
            if (data.player.TryGetPlayer(out var player) && player.Lookup == senderID && SNet.IsMaster)
            {
                if (!DamageInfoReceivers.Contains(senderID))
                {
                    DamageInfoReceivers.Add(senderID);
                }
            }
        }

        public static void RequestRegistDamageInfoReceiver()
        {
            if (SNet.IsMaster)
            {
                return;
            }
            pRegistDamageInfoReceiver data = new(SNet.LocalPlayer);
            NetworkAPI.InvokeEvent(typeof(pRegistDamageInfoReceiver).FullName, data, SNet.Master, SNet_ChannelType.GameNonCritical);
        }

        public static void ProcessDamageInfo(BasicDamageInfo damageInfo)
        {
            if (!damageInfo._player.TryGetPlayer(out var player))
            {
                return;
            }
            if (!damageInfo._enemy.TryGet(out var enemy))
            {
                return;
            }
            var limb = enemy.Damage.DamageLimbs[damageInfo._limbID];
            float realDamage = damageInfo._rawDamage;
            bool isCrit = false;
            bool isArmor = false;
            bool isBackMulti = false;
            bool isSleepMulti = damageInfo._isSleepMulti;
            bool isImmortal = enemy.Damage.IsImortal;
            bool allowDirectionalBonus = damageInfo._allowDirectionalBonus;
            float PrecisionMulti = damageInfo._precisionMulti;
            float SleeperMulti = damageInfo._sleeperMulti;
            float BackStubberMulti = damageInfo._backstabberMulti;
            Vector3 Direction = damageInfo._direction;
            Vector3 Position = damageInfo._position;
            DamageType damageType = damageInfo._damageType;
            DamageSource damageSource = damageInfo._damageSource;
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
                        if (SNet.IsMaster && DamageInfoReceivers.Contains(player.Lookup))
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

            DamageDisplay.Instance.UpdateBasicDamageInfo(limb, isImmortal, isArmor, isCrit, isBackMulti, IsSleepMulti, realDamage);
        }

        private static bool ShowSentryDamage;

        private static List<ulong> DamageInfoReceivers = new();

        public struct pRegistDamageInfoReceiver
        {
            public pRegistDamageInfoReceiver(SNet_Player player)
            {
                this.player = new();
                this.player.SetPlayer(player);
            }

            public SNetStructs.pPlayer player;
        }

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
                _player = new();
                _player.SetPlayer(player);
                _damageType = damageType;
                _damageSource = damageSource;
                _enemy = new();
                _enemy.Set(limb.m_base.Owner);
                _limbID = limb.m_limbID;
                _position = position;
                _direction = direction;
                _rawDamage = rawDamage;
                _allowDirectionalBonus = allowDirectionalBonus;
                _staggerMulti = staggerMulti;
                _precisionMulti = precisionMulti;
                _backstabberMulti = backstabberMulti;
                _sleeperMulti = sleeperMulti;
                _isSleepMulti = isSleepMulti;
            }

            public SNetStructs.pPlayer _player;

            public pEnemyAgent _enemy;

            public float _rawDamage;

            public int _limbID;

            public bool _allowDirectionalBonus;

            public bool _isSleepMulti;

            public float _staggerMulti;

            public float _precisionMulti;

            public float _backstabberMulti;

            public float _sleeperMulti;

            public DamageType _damageType;

            public DamageSource _damageSource;

            public Vector3 _position;

            public Vector3 _direction;
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
}
