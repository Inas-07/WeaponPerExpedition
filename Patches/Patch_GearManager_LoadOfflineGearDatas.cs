using HarmonyLib;
using Gear;
using Player;
using static Gear.GearIDRange;

namespace WeaponPerExpedition.Patches
{
    [HarmonyPatch]
    internal class Patch_GearManager_LoadOfflineGearDatas
    {
        // called on both host and client side
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GearManager), nameof(GearManager.LoadOfflineGearDatas))]
        private static void Pre_GearManager_LoadOfflineGearDatas(GearManager __instance)
        {
            foreach(var gearSlot in ExpeditionGearManager.Current.gearSlots)
            {
                foreach (GearIDRange gearIDRange in __instance.m_gearPerSlot[(int)gearSlot.inventorySlot])
                {
                    uint playerOfflineDBPID = ExpeditionGearManager.GetOfflineGearPID(gearIDRange);
                    gearSlot.loadedGears.Add(playerOfflineDBPID, gearIDRange);
                }
            }
        }
    }
}
