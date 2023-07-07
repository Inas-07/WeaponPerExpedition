using System.Collections.Generic;

namespace WeaponPerExpedition
{
    public enum Mode { ALLOW, DISALLOW }

    public class ExpeditionGears
    {
        public eRundownTier Tier { get; set; } = eRundownTier.TierA;

        public int ExpeditionIndex { get; set; } = -1;

        public Mode Mode { get; set; } = Mode.DISALLOW;

        public List<uint> GearIds { get; set; } = new() { 0u };
    }

    public class RundownExpeditionGears
    {
        public uint RundownID { get; set; } = 0u;

        public List<ExpeditionGears> ExpeditionGears { set; get; } = new() { new() };

        public RundownExpeditionGears() { }
    }
}
