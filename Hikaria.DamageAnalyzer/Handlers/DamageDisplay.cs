using Hikaria.DamageAnalyzer.Config;
using Hikaria.DamageAnalyzer.Lang;
using Hikaria.DamageAnalyzer.Utils;
using System;
using System.Text;
using UnityEngine;

namespace Hikaria.DamageAnalyzer.Handlers
{
    public class DamageDisplay : MonoBehaviour
    {
        private void Awake()
        {
            Instance = this;
            if (ConfigManager.Language.Value == ConfigManager.LanguageType.zh_CN)
            {
                language = new SimplifiedChinese();
            }
            else
            {
                language = new English();
            }
            _showHints = ConfigManager.ShowHints.Value;
            _showHPBar = ConfigManager.ShowHPBar.Value;
            _showName = ConfigManager.ShowName.Value;
            _showPos = ConfigManager.ShowPos.Value;
            _lastingTime = ConfigManager.LastingTime.Value;
            _barFillChar_Remaining = ConfigManager.BarFillChar_Remaining.Value;
            _barFillChar_Losted = ConfigManager.BarFillChar_Losted.Value;
            barFontSize = ConfigManager.BarFontSize.Value;
            barLength = ConfigManager.BarLength.Value;
            labelheight = ConfigManager.LabelHeight.Value;
            labelHeightOffset = ConfigManager.LabelHeightOffset.Value;
            labelwidth = ConfigManager.LabelWidth.Value;
            labelWidthOffset = ConfigManager.LabelWidthOffset.Value;
            showDamage = ConfigManager.ShowDamage.Value;
            hintFontSize = ConfigManager.HintFontSize.Value;
        }

        private void FixedUpdate()
        {
            if (timer <= _lastingTime)
            {
                timer += Time.fixedDeltaTime;
            }
            else
            {
                showDamageInfo = false;
                return;
            }
            if (updateTimer >= 0.1f)
            {
                if (showDamageInfo)
                {
                    UpdateDamageInfo();
                }
                updateTimer = 0f;
            }
            updateTimer += Time.fixedDeltaTime;
        }

        private void OnGUI()
        {
            if (showDamageInfo)
            {
                GUI.Label(new Rect(Width, Height, labelwidth, labelheight), damageInfo);
            }
        }

        private void RestartTimer()
        {
            timer = 0f;
            showDamageInfo = true;
        }

        public void UpdateBasicDamageInfo(Dam_EnemyDamageLimb dam_EnemyDamageLimb, bool isImmortal, bool isArmor, bool isCrit, bool isBackMulti, bool isSleepMulti, float damage)
        {
            _dmgBase = dam_EnemyDamageLimb.m_base;
            _limbName = TranslateManager.EnemyLimb(dam_EnemyDamageLimb.name);
            _enemyName = TranslateManager.EnemyName(_dmgBase.Owner.EnemyDataID);
            _isImmortal = isImmortal;
            _isArmor = isArmor;
            _isCrit = isCrit;
            _isBackMulti = isBackMulti;
            _isSleepMulti = isSleepMulti;
            _damage = damage;

            RestartTimer();
        }

        private void UpdateDamageInfo()
        {
            bool hasDmgBase = _dmgBase != null;
            if (hasDmgBase)
            {
                _health = _dmgBase.Health;
                _healthMax = _dmgBase.HealthMax;
            }
            else
            {
                _health = 0f;
            }
            StringBuilder sb = new StringBuilder(500);
            _health = Math.Max(_health, 0f);
            _health = (float)Math.Round(_health, 1);
            if (hasDmgBase)
            {
                _isKill = !hasDmgBase || _dmgBase.Health <= 0f;
            }
            else
            {
                _isKill = true;
            }
            sb.Append($"<size={hintFontSize}>");
            if (_showName)
            {
                sb.Append(_enemyName);
            }
            if (_showPos)
            {
                if (_showName)
                {
                    sb.Append($" ({_limbName})");
                }
                else
                {
                    sb.Append($"{language.HITPOSITION_DESC}: {_limbName}");
                }
            }
            sb.Append("</size>\n");
            if (_showHPBar)
            {
                sb.Append($"<size={barFontSize}>HP: |-");
                int num = (int)(_health / _healthMax * barLength);
                if (num == 0 && !_isKill)
                {
                    num = 1;
                }
                sb.Append(new string(_barFillChar_Remaining[0], num));
                sb.Append(new string(_barFillChar_Losted[0], barLength - num));
                sb.Append("-| </size>");
            }
            else
            {
                sb.Append($"<size={barFontSize}>HP: </size>");
            }
            sb.AppendLine($"<size={barFontSize}>[{_health} / {_healthMax}]</size>");
            if (_showHints)
            {
                sb.Append($"<size={hintFontSize}>");
                if (showDamage)
                {
                    sb.AppendLine($"HP -{_damage.ToString("0.00")}");
                }
                if (_isCrit)
                {
                    sb.AppendLine(language.CRIT);
                }
                if (_isBackMulti)
                {
                    sb.AppendLine(language.BACKMULTI);
                }
                if (_isSleepMulti)
                {
                    sb.AppendLine(language.SLEEPMULTI);
                }
                if (_isImmortal)
                {
                    sb.AppendLine(language.IMMORTAL);
                }
                if (_isArmor)
                {
                    sb.AppendLine(language.ARMOR);
                }
                if (_isKill)
                {
                    sb.AppendLine(language.KILL);
                }
                sb.Append("</size>");
            }
            damageInfo = sb.ToString();
        }

        private bool showDamageInfo;

        private string damageInfo = "Initializing...";

        private float Width => Screen.width / 2f - labelwidth / 2f + labelWidthOffset;
        private float Height => Screen.height / 4f - labelheight / 2f + labelHeightOffset;

        internal LanguageBase language;

        private int barFontSize = 0;

        private int hintFontSize = 0;

        private int barLength = 0;

        private int labelHeightOffset = 0;

        private int labelWidthOffset = 0;

        private int labelwidth = 0;

        private int labelheight = 0;

        private bool _showHints;

        private string _barFillChar_Remaining;

        private string _barFillChar_Losted;

        private bool _showHPBar;

        private bool _showName;

        private bool _showPos;

        private Dam_EnemyDamageBase _dmgBase;

        private float _healthMax;

        private float _health;

        private bool _isImmortal;

        private string _limbName = "";

        private string _enemyName = "";

        private bool _isKill;

        private bool _isArmor;

        private bool _isCrit;

        private bool _isBackMulti;

        private bool _isSleepMulti;

        private float _damage = 0f;

        private bool showDamage;

        public static DamageDisplay Instance { get; private set; }

        private float timer = 0f;

        private float _lastingTime = 5f;

        private float updateTimer = 0f;
    }
}
