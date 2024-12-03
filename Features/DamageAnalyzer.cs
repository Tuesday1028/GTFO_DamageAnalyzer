using Agents;
using Enemies;
using Gear;
using Hikaria.DamageAnalyzer.Handlers;
using Hikaria.DamageAnalyzer.Managers;
using Player;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Core.Models;
using TheArchive.Loader;
using TheArchive.Utilities;
using UnityEngine;
using static Hikaria.DamageAnalyzer.Managers.DamageInfoManager;

namespace Hikaria.DamageAnalyzer.Features;

[DisallowInGameToggle]
[EnableFeatureByDefault]
public class DamageAnalyzer : Feature
{
    public override string Name => "Damage Analyzer";

    public override bool InlineSettingsIntoParentMenu => true;

    public static new ILocalizationService Localization { get; set; }

    [FeatureConfig]
    public static DamageAnalyzerSetting Settings { get; set; }

    public class DamageAnalyzerSetting
    {
        [FSDisplayName("持续时间")]
        public float LastingTime { get => DamageInfoUpdater.LastingTime; set => DamageInfoUpdater.LastingTime = value; }
        [FSDisplayName("生命值条文本大小")]
        public int BarFontSize { get => DamageInfoUpdater.BarFontSize; set => DamageInfoUpdater.BarFontSize = value; }
        [FSDisplayName("提示文本大小")]
        public int HintFontSize { get => DamageInfoUpdater.HintFontSize; set => DamageInfoUpdater.HintFontSize = value; }
        [FSDisplayName("生命值条文本长度")]
        public int BarLength { get => DamageInfoUpdater.BarLength; set => DamageInfoUpdater.BarLength = value; }
        [FSDisplayName("显示位置横向偏移量")]
        public int OffsetX
        {
            get
            {
                if (DamageInfoUpdater.TextMesh == null)
                {
                    return DamageInfoUpdater.OffsetX;
                }
                return (int)DamageInfoUpdater.TextMesh.transform.localPosition.x;
            }
            set
            {
                DamageInfoUpdater.OffsetX = value;
                if (DamageInfoUpdater.TextMesh == null)
                {
                    return;
                }
                DamageInfoUpdater.TextMesh.transform.localPosition = new(value, OffsetY, 0f);
            }
        }
        [FSDisplayName("显示位置纵向偏移量")]
        public int OffsetY
        {
            get
            {
                if (DamageInfoUpdater.TextMesh == null)
                {
                    return DamageInfoUpdater.OffsetY;
                }
                return (int)DamageInfoUpdater.TextMesh.transform.localPosition.y;
            }
            set
            {
                DamageInfoUpdater.OffsetY = value;
                if (DamageInfoUpdater.TextMesh == null)
                {
                    return;
                }
                DamageInfoUpdater.TextMesh.transform.localPosition = new(OffsetX, value, 0f);
            }
        }
        [FSDisplayName("显示提示")]
        public bool ShowHints { get => DamageInfoUpdater.ShowHints; set => DamageInfoUpdater.ShowHints = value; }
        [FSDisplayName("显示生命值条")]
        public bool ShowHPBar { get => DamageInfoUpdater.ShowHPBar; set => DamageInfoUpdater.ShowHPBar = value; }
        [FSDisplayName("显示敌人名称")]
        public bool ShowName { get => DamageInfoUpdater.ShowName; set => DamageInfoUpdater.ShowName = value; }
        [FSDisplayName("显示命中部位")]
        public bool ShowLimb { get => DamageInfoUpdater.ShowLimb; set => DamageInfoUpdater.ShowLimb = value; }
        [FSDisplayName("显示造成的伤害")]
        public bool ShowDamage { get => DamageInfoUpdater.ShowDamage; set => DamageInfoUpdater.ShowDamage = value; }
        [FSDisplayName("显示哨戒炮造成的伤害")]
        [FSDescription("显示哨戒炮的命中信息, 仅当房主也安装了本插件才能显示")]
        public bool ShowSentryDamage { get => DamageInfoManager.ShowSentryDamage; set => DamageInfoManager.ShowSentryDamage = value; }
        [FSDisplayName("生命值剩余表示字符")]
        public string BarFillChar_Remaining { get => DamageInfoUpdater.BarFillChar_Remaining; set => DamageInfoUpdater.BarFillChar_Remaining = value; }
        [FSDisplayName("生命值失去表示字符")]
        public string BarFillChar_Lost { get => DamageInfoUpdater.BarFillChar_Lost; set => DamageInfoUpdater.BarFillChar_Lost = value; }
        [FSDisplayName("文本颜色")]
        public SColor TextColor
        {
            get
            {
                if (DamageInfoUpdater.TextMesh == null)
                    return DamageInfoUpdater.TextColor.ToSColor();
                return DamageInfoUpdater.TextMesh.color.ToSColor();
            }
            set
            {
                DamageInfoUpdater.TextColor = value.ToUnityColor();
                if (DamageInfoUpdater.TextMesh == null)
                {
                    return;
                }
                DamageInfoUpdater.TextMesh.color = value.ToUnityColor();
            }
        }
    }

    public override void Init()
    {
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<DamageInfoUpdater>();
        DamageInfoManager.Setup();
    }

    [ArchivePatch(typeof(PUI_Watermark), nameof(PUI_Watermark.UpdateWatermark))]
    private class PUI_Watermark__UpdateWatermark__Patch
    {
        private static bool IsSetup;

        private static void Postfix()
        {
            if (!IsSetup)
            {
                GameObject obj = new("DamageAnalyzer");
                obj.transform.position = new(Screen.width / 2f, Screen.height / 2f);
                GameObject.DontDestroyOnLoad(obj);
                obj.AddComponent<DamageInfoUpdater>();

                IsSetup = true;
            }
        }
    }

    #region Enemy Damage
    [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
    private class BulletWeapon__BulletHit__Patch
    {
        private static void Prefix(Weapon.WeaponHitData weaponRayData, bool doDamage, float additionalDis)
        {
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
    }

    [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
    private class Dam_EnemyDamageLimb__MeleeDamage__Patch
    {
        private static void Prefix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent, Vector3 position, Vector3 direction, float staggerMulti = 1f, float precisionMulti = 1f, float backstabberMulti = 1f, float sleeperMulti = 1f)
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

        private static void Postfix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent, Vector3 position, Vector3 direction, float staggerMulti = 1f, float precisionMulti = 1f, float backstabberMulti = 1f, float sleeperMulti = 1f)
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
    }


    [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
    private class Dam_EnemyDamageLimb__BulletDamage__Patch
    {
        private static void Postfix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent, Vector3 position, Vector3 direction, bool allowDirectionalBonus, float staggerMulti = 1f, float precisionMulti = 1f)
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
    }


    [ArchivePatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.FireBullet))]
    private class SentryGunInstance_Firing_Bullets__FireBullet__Patch
    {
        private static void Prefix()
        {
            IsSentryGunFire = true;
        }

        private static void Postfix()
        {
            IsSentryGunFire = false;
        }
    }

    [ArchivePatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireShotgunSemi))]
    private class SentryGunInstance_Firing_Bullets__UpdateFireShotgunSemi__Patch
    {
        private static void Prefix(SentryGunInstance_Firing_Bullets __instance)
        {
            if (Clock.Time > __instance.m_fireBulletTimer)
                IsSentryGunFire = true;

        }
        private static void Postfix()
        {
            IsSentryGunFire = false;
        }
    }

    [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
    private class LocalPlayerAgent__Setup__Patch
    {
        private static void Postfix(LocalPlayerAgent __instance)
        {
            LocalAgentGlobalID = __instance.GlobalID;
        }
    }
#endregion
}
