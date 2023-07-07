using System.IO;
using GameData;
using Gear;
using GTFO.API;
using GTFO.API.Utilities;
using Player;
using WeaponPerExpedition.JSON;
using System.Collections.Generic;
using static Gear.GearIDRange;
using System.Linq;

namespace WeaponPerExpedition
{
    internal class ExpeditionGearManager
    {
        public static ExpeditionGearManager Current { get; private set; } = new();

        private GearManager vanillaGearManager = null;

        public (uint RundownId, eRundownTier Tier, int ExpeditionIndex) CurrentExpedition { get; private set; } = (0u, eRundownTier.TierA, 0);

        private Dictionary<uint, Dictionary<(eRundownTier Tier, int ExpeditionIndex), ExpeditionGears>> ExpeditionGearConfigs = new();

        public string CONFIG_DIR_PATH { get; private set; } = Path.Combine(MTFOUtil.CustomPath, "WeaponPerExpedition");

        private Mode mode = Mode.DISALLOW;

        private HashSet<uint> GearIDs = new();

        private readonly LiveEditListener liveEditListener;

        internal readonly List<(InventorySlot inventorySlot, Dictionary<uint, GearIDRange> loadedGears)> gearSlots = new() { 
            (InventorySlot.GearStandard, new()),
            (InventorySlot.GearSpecial, new()),
            (InventorySlot.GearMelee, new()),
            (InventorySlot.GearClass, new()),
        };

        public void ClearLoadedGears()
        {
            foreach (var slot in gearSlots)
            {
                vanillaGearManager.m_gearPerSlot[(int)slot.inventorySlot].Clear();
            }
        }

        public static uint GetOfflineGearPID(GearIDRange gearIDRange)
        {
            string itemInstanceId = gearIDRange.PlayfabItemInstanceId;
            if (!itemInstanceId.Contains("OfflineGear_ID_"))
            {
                WPELogger.Error($"Find PlayfabItemInstanceId without substring 'OfflineGear_ID_'! {itemInstanceId}");
                return 0;
            }

            try
            {
                uint offlineGearPersistentID = uint.Parse(itemInstanceId.Substring("OfflineGear_ID_".Length));
                return offlineGearPersistentID;
            }
            catch
            {
                WPELogger.Error("Caught exception while trying to parse persistentID of PlayerOfflineGearDB from GearIDRange, which means itemInstanceId could be ill-formated");
                return 0;
            }
        }

        private bool IsGearAllowed(uint playerOfflineGearDBPID)
        {
            switch(mode)
            {
                case Mode.ALLOW: return GearIDs.Contains(playerOfflineGearDBPID);
                case Mode.DISALLOW: return !GearIDs.Contains(playerOfflineGearDBPID);
                default:
                    WPELogger.Error($"Unimplemented Mode: {mode}, will allow gears anyway...");
                    return true;
            }
        }

        private void AddGearForCurrentExpedition()
        {
            foreach(var slot in gearSlots)
            {
                var vanillaSlot = vanillaGearManager.m_gearPerSlot[(int)slot.inventorySlot];
                var loadedGearsInCategory = slot.loadedGears;

                if(loadedGearsInCategory.Count == 0)
                {
                    WPELogger.Debug($"No gear has been loaded for {slot.inventorySlot}.");
                    continue;
                }

                foreach (uint offlineGearPID in loadedGearsInCategory.Keys)
                {
                    if(IsGearAllowed(offlineGearPID))
                    {
                        vanillaSlot.Add(loadedGearsInCategory[offlineGearPID]);
                    }
                }

                if(vanillaSlot.Count == 0)
                {
                    WPELogger.Error($"No gear is allowed for {slot.inventorySlot}, there must be at least 1 allowed gear!");
                    vanillaSlot.Add(loadedGearsInCategory.First().Value);
                }
            }
        }

        private void ResetPlayerSelectedGears()
        {
            vanillaGearManager.RescanFavorites();
            foreach (var gearSlot in gearSlots)
            {
                var inventorySlotIndex = (int)gearSlot.inventorySlot;

                if (vanillaGearManager.m_lastEquippedGearPerSlot[inventorySlotIndex] != null)
                    PlayerBackpackManager.EquipLocalGear(vanillaGearManager.m_lastEquippedGearPerSlot[inventorySlotIndex]);
                else if (vanillaGearManager.m_favoriteGearPerSlot[inventorySlotIndex].Count > 0)
                    PlayerBackpackManager.EquipLocalGear(vanillaGearManager.m_favoriteGearPerSlot[inventorySlotIndex][0]);
                else if (vanillaGearManager.m_gearPerSlot[inventorySlotIndex].Count > 0)
                    PlayerBackpackManager.EquipLocalGear(vanillaGearManager.m_gearPerSlot[inventorySlotIndex][0]);
            }
        }

        private void LoadWPEConfigForCurrentExpedition()
        {
            var (rundownId, expTier, expIndexInTier) = CurrentExpedition;

            GearIDs.Clear();
            mode = Mode.DISALLOW;
            if (ExpeditionGearConfigs.ContainsKey(rundownId) && ExpeditionGearConfigs[rundownId].ContainsKey((expTier, expIndexInTier)))
            {
                mode = ExpeditionGearConfigs[rundownId][(expTier, expIndexInTier)].Mode;
                ExpeditionGearConfigs[rundownId][(expTier, expIndexInTier)].GearIds.ForEach(id => GearIDs.Add(id));
            }
        }

        public void OnLevelSelected(eRundownTier expTier, int expIndexInTier)
        {
            if(!RundownManager.TryGetIdFromLocalRundownKey(RundownManager.ActiveRundownKey, out var rundownId))
            {
                WPELogger.Error("Failed to get active rundown ID, will fall back to rundown Id 1");
                rundownId = 1u;
            }

            // logger's output doesn't match the actually selected expedition, but everything works fine :|
            //WPELogger.Warning($"OnLevelSelected: {CurrentExpedition}");
            CurrentExpedition = (rundownId, expTier, expIndexInTier);

            LoadWPEConfigForCurrentExpedition();
            ClearLoadedGears();
            AddGearForCurrentExpedition();
            ResetPlayerSelectedGears();
        }

        private void OnManagersSetup()
        {
            vanillaGearManager = GearManager.Current;
        }

        private void AddConf(RundownExpeditionGears conf)
        {
            if (conf == null) return;

            Dictionary<(eRundownTier, int), ExpeditionGears> rundownExpeditionConfig = null;
            if (!ExpeditionGearConfigs.ContainsKey(conf.RundownID))
            {
                rundownExpeditionConfig = new();
                ExpeditionGearConfigs[conf.RundownID] = rundownExpeditionConfig; 
            }
            else
            {
                WPELogger.Log($"Replaced rundown ID {conf.RundownID}");
                rundownExpeditionConfig = ExpeditionGearConfigs[conf.RundownID];
            }

            conf.ExpeditionGears.ForEach(expGearConf => rundownExpeditionConfig[(expGearConf.Tier, expGearConf.ExpeditionIndex)] = expGearConf);
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            WPELogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                RundownExpeditionGears conf = Json.Deserialize<RundownExpeditionGears>(content);
                AddConf(conf);
            });
        }

        private ExpeditionGearManager() 
        {
            if (!Directory.Exists(CONFIG_DIR_PATH))
            {
                Directory.CreateDirectory(CONFIG_DIR_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_DIR_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new RundownExpeditionGears()));
                file.Flush();
                file.Close();
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_DIR_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                RundownExpeditionGears conf = Json.Deserialize<RundownExpeditionGears>(content);
                AddConf(conf);
            }

            liveEditListener = LiveEdit.CreateListener(CONFIG_DIR_PATH, "*.json", true);
            liveEditListener.FileChanged += FileChanged;

            //LevelAPI.OnLevelSelected += OnLevelSelected; 

            EventAPI.OnManagersSetup += OnManagersSetup;
        }

        public void Init() { }
    
        static ExpeditionGearManager() { }
    }
}
