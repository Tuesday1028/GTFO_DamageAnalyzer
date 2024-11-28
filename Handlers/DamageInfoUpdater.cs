using BepInEx.Unity.IL2CPP.Utils;
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using static Hikaria.DamageAnalyzer.Managers.TranslateManager;

namespace Hikaria.DamageAnalyzer.Handlers;

public class DamageInfoUpdater : MonoBehaviour
{
    private void Awake()
    {
        Instance = this;

        TextMesh = GameObject.Instantiate(WatermarkTextPrefab);
        TextMesh.rectTransform.SetParent(WatermarkTextPrefab.rectTransform.parent);
        TextMesh.rectTransform.localPosition = new(OffsetX, OffsetY);
        TextMesh.rectTransform.sizeDelta = new(1920, TextMesh.rectTransform.sizeDelta.y);
        TextMesh.color = TextColor;
        TextMesh.alignment = TextAlignmentOptions.TopLeft;
        TextMesh.SetText(string.Empty);
        TextMesh.rectTransform.localScale = Vector3.zero;

        this.StartCoroutine(UpdateTextCoroutine());
    }

    private void FixedUpdate()
    {
        if (timer >= LastingTime)
        {
            TextMesh.rectTransform.localScale = Vector3.zero;
            Status = false;
        }
        if (Status)
        {
            timer += Time.fixedDeltaTime;
        }
    }

    private IEnumerator UpdateTextCoroutine()
    {
        var yielder = new WaitForSecondsRealtime(0.05f);
        while (true)
        {
            if (Status)
                UpdateDamageInfo();
            yield return yielder;
        }
    }

    private void RestartTimer()
    {
        timer = 0;
        Status = true;
        TextMesh.rectTransform.localScale = Vector3.one;
    }

    public void UpdateBasicDamageInfo(Dam_EnemyDamageLimb dam_EnemyDamageLimb, bool isImmortal, bool isArmor, bool isCrit, bool isBackMulti, bool isSleepMulti, float damage)
    {
        _dmgBase = dam_EnemyDamageLimb.m_base;
        _limbName = EnemyLimb(dam_EnemyDamageLimb.name);
        _enemyName = EnemyName(_dmgBase.Owner.EnemyDataID);
        _isImmortal = isImmortal;
        _isArmor = isArmor;
        _isCrit = isCrit;
        _isBackMulti = isBackMulti;
        _isSleepMulti = isSleepMulti;
        _damage = isImmortal ? 0f : damage;

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
        sb.Append($"<size={HintFontSize}>");
        if (ShowName)
        {
            sb.Append(_enemyName);
        }
        if (ShowLimb)
        {
            if (ShowName)
            {
                sb.Append($" ({_limbName})");
            }
            else
            {
                sb.Append($"{HITPOSITION_DESC}: {_limbName}");
            }
        }
        sb.Append("</size>\n");
        if (ShowHPBar)
        {
            sb.Append($"<size={BarFontSize}>HP: |-");
            int num = (int)(_health / _healthMax * BarLength);
            if (num == 0 && !_isKill)
            {
                num = 1;
            }
            sb.Append(BarFillChar_Remaining[0], num);
            sb.Append(BarFillChar_Lost[0], BarLength - num);
            sb.Append("-| </size>");
        }
        else
        {
            sb.Append($"<size={BarFontSize}>HP: </size>");
        }
        sb.AppendLine($"<size={BarFontSize}>[{_health} / {_healthMax}]</size>");
        if (ShowHints)
        {
            sb.Append($"<size={HintFontSize}>");
            if (ShowDamage)
            {
                sb.AppendLine($"HP -{_damage.ToString("0.00")}");
            }
            if (_isCrit)
            {
                sb.AppendLine(CRIT);
            }
            if (_isBackMulti)
            {
                sb.AppendLine(BACKMULTI);
            }
            if (_isSleepMulti)
            {
                sb.AppendLine(SLEEPMULTI);
            }
            if (_isImmortal)
            {
                sb.AppendLine(IMMORTAL);
            }
            if (_isArmor)
            {
                sb.AppendLine(ARMOR);
            }
            if (_isKill)
            {
                sb.AppendLine(KILL);
            }
            sb.Append("</size>");
        }
        TextMesh.SetText(sb.ToString());
        sb.Clear();
    }

    private bool Status;
    private float timer = 0;
    private static StringBuilder sb = new(500);

    private Dam_EnemyDamageBase _dmgBase;
    private float _healthMax;
    private float _health;
    private bool _isImmortal;
    private string _limbName = string.Empty;
    private string _enemyName = string.Empty;
    private bool _isKill;
    private bool _isArmor;
    private bool _isCrit;
    private bool _isBackMulti;
    private bool _isSleepMulti;
    private float _damage = 0f;

    public static float LastingTime = 2.5f;
    public static int BarFontSize = 12;
    public static int HintFontSize = 12;
    public static int BarLength = 80;
    public static int OffsetX = 970;
    public static int OffsetY = 790;
    public static bool ShowHints = true;
    public static string BarFillChar_Remaining = "#";
    public static string BarFillChar_Lost = "-";
    public static bool ShowHPBar = true;
    public static bool ShowName = true;
    public static bool ShowLimb = true;
    public static bool ShowDamage = true;
    public static Color TextColor = new(1f, 1f, 0.8823f, 0.7059f);

    public static DamageInfoUpdater Instance { get; private set; }
    public static TextMeshPro TextMesh;
    public static TextMeshPro WatermarkTextPrefab => GuiManager.WatermarkLayer.m_watermark.m_watermarkText;
}
