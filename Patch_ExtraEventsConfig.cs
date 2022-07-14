﻿using HarmonyLib;
using Enemies;
using LevelGeneration;
using GameData;
using AIGraph;
using Globals;
using System.Collections.Generic;
using LEGACY.Utilities;

namespace LEGACY.Patch
{
    [HarmonyPatch]
    class Patch_ExtraEventsConfig
    {
        private static bool SetTerminalCommand_Custom(WardenObjectiveEventData eventToTrigger, eWardenObjectiveEventTrigger trigger)
        {
            LG_LayerType layer = eventToTrigger.Layer;
            eLocalZoneIndex localIndex = eventToTrigger.LocalIndex;
            eDimensionIndex dimensionIndex = eventToTrigger.DimensionIndex;
            LG_Zone terminalZone = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out terminalZone);
            if (terminalZone == null)
            {
                Logger.Error("Failed to get terminal in zone {0}, layer {1}, dimension {2}.", localIndex, layer, dimensionIndex);
                return true;
            }

            if (terminalZone.TerminalsSpawnedInZone == null)
            {
                Logger.Error("ExtraEventsConfig: terminalZone.TerminalsSpawnedInZone == null");
                return true;
            }

            if (terminalZone.TerminalsSpawnedInZone.Count < 1)
            {
                Logger.Error("ExtraEventsConfig: No terminal spawns in the specified zone!");
                return true;
            }

            if (eventToTrigger.Count >= terminalZone.TerminalsSpawnedInZone.Count)
            {
                Logger.Error("ExtraEventsConfig: Invalid event.Count: 0 < event.Count < TerminalsSpawnedInZone.Count should suffice.");
                return true;
            }

            LG_ComputerTerminal terminal = terminalZone.TerminalsSpawnedInZone[eventToTrigger.Count];
            if (eventToTrigger.Enabled == true)
            {
                terminal.TrySyncSetCommandShow(eventToTrigger.TerminalCommand);
            }
            else
            {
                terminal.TrySyncSetCommandHidden(eventToTrigger.TerminalCommand);
            }

            WardenObjectiveManager.DisplayWardenIntel(eventToTrigger.Layer, eventToTrigger.WardenIntel);
            Logger.Warning("Succeed setting terminal command visibility!");
            return false;
        }

        //private static bool LockSecurityDoor_Custom(WardenObjectiveEventData eventToTrigger, eWardenObjectiveEventTrigger trigger)
        //{
        //    // -===============================
        //    //      some research
        //    // -===============================
        //    //LG_LayerType layer = eventToTrigger.Layer;
        //    //eLocalZoneIndex localIndex = eventToTrigger.LocalIndex;
        //    //eDimensionIndex dimensionIndex = eventToTrigger.DimensionIndex;
        //    //LG_Zone zoneToBeLocked = null;
        //    //Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out zoneToBeLocked);
        //    //if (zoneToBeLocked == null)
        //    //{
        //    //    Logger.Error("Failed to get target zone {0}, layer {1}, dimension {2}.", localIndex, layer, dimensionIndex);
        //    //    return true;
        //    //}
            
        //    //zoneToBeLocked.m_sourceGate.SpawnedDoor.

        //    // -===============================
        //    // possible implementation.
        //    // -===============================
        //    //LG_LayerType layer = eventToTrigger.Layer;
        //    //eLocalZoneIndex localIndex = eventToTrigger.LocalIndex;
        //    //eDimensionIndex dimensionIndex = eventToTrigger.DimensionIndex;
        //    //LG_Zone zoneToBeLocked = null;
        //    //Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out zoneToBeLocked);
        //    //if (zoneToBeLocked == null)
        //    //{
        //    //    Logger.Error("Failed to get target zone {0}, layer {1}, dimension {2}.", localIndex, layer, dimensionIndex);
        //    //    return true;
        //    //}

        //    //Builder.CurrentFloor

        //    return true;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger), new System.Type[] {
            typeof(WardenObjectiveEventData),
            typeof(eWardenObjectiveEventTrigger),
            typeof(bool),
            typeof(float)
        })]
        private static bool Pre_CheckAndExecuteEventsOnTrigger(WardenObjectiveManager __instance,
            WardenObjectiveEventData eventToTrigger,
            eWardenObjectiveEventTrigger trigger,
            bool ignoreTrigger = false,
            float currentDuration = 0.0f)
        {
            switch(eventToTrigger.Type)
            {
                case eWardenObjectiveEventType.SetTerminalCommand:
                    return SetTerminalCommand_Custom(eventToTrigger, trigger);

                //case eWardenObjectiveEventType.LockSecurityDoor:
                //    return LockSecurityDoor_Custom(eventToTrigger, trigger);

                default: return true;
            }
        }
    }
}