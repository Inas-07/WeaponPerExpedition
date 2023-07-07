using GameData;
using HarmonyLib;
namespace WeaponPerExpedition.Patches
{
    [HarmonyPatch]
    internal class Patch_RundownManager
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RundownManager), nameof(RundownManager.SetActiveExpedition))]
        private static void Post_RundownManager_SetActiveExpedition(RundownManager __instance, pActiveExpedition expPackage, ExpeditionInTierData expTierData)
        {
            if (expPackage.tier == eRundownTier.Surface) return;
            var tier = expPackage.tier;
            var expeditionIndex = expPackage.expeditionIndex;

            ExpeditionGearManager.Current.OnLevelSelected(tier, expeditionIndex);
        }
    }
}
