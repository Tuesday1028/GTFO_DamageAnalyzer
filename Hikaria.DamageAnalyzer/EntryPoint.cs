using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Hikaria.DamageAnalyzer.Config;
using Hikaria.DamageAnalyzer.Handlers;
using Hikaria.DamageAnalyzer.Managers;
using Hikaria.DamageAnalyzer.Utils;
using Il2CppInterop.Runtime.Injection;

namespace Hikaria.DamageAnalyzer
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    internal class EntryPoint : BasePlugin
    {
        public override void Load()
        {
            Instance = this;

            ConfigManager.Setup();
            TranslateManager.Setup();
            DamageInfoManager.Init();

            ClassInjector.RegisterTypeInIl2Cpp<DamageDisplay>();

            m_Harmony = new Harmony(PluginInfo.GUID);
            m_Harmony.PatchAll();

            Logs.LogMessage("OK");
        }

        public static EntryPoint Instance { get; private set; }

        private Harmony m_Harmony;
    }
}
