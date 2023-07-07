using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace WeaponPerExpedition
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(AUTHOR + "." + PLUGIN_NAME, PLUGIN_NAME, VERSION)]
    
    public class EntryPoint: BasePlugin
    {
        public const string AUTHOR = "Inas";
        public const string PLUGIN_NAME = "WeaponPerExpedition";
        public const string VERSION = "1.0.0";

        private Harmony m_Harmony;
        
        public override void Load()
        {
            m_Harmony = new Harmony("WeaponPerExpedition");
            m_Harmony.PatchAll();

            ExpeditionGearManager.Current.Init();
        }
    }
}

