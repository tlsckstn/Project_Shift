namespace ProjectShift.Stage
{
    /// <summary>The computer part a chapter's stage select page is drawn as.</summary>
    public enum Zone { Ram, Bus, Cores, Alu }

    /// <summary>How a stage slot looks on the select page.</summary>
    public enum SlotState { Locked, Open, Cleared }

    /// <summary>Pure rules mapping chapters and stages to zone names, labels and slot states.</summary>
    public static class ChapterZones
    {
        // Inward along the data path: the memory bus (Night 2) meets the IMC, then L3 to L1, then the cores, whose ALU is
        // Night 4.
        private static readonly string[] CoresLabels = { "IMC", "L3", "L2", "L1", "CORE1", "CORE2", "CORE3", "CORE4" };

        /// <summary>Zone of a 0-based chapter: Ram, Bus, Cores, then Alu for every later chapter.</summary>
        public static Zone ZoneOf(int chapterIndex)
        {
            return chapterIndex >= (int)Zone.Alu ? Zone.Alu : (Zone)chapterIndex;
        }

        /// <summary>Display name of a zone.</summary>
        public static string ZoneName(Zone zone)
        {
            switch (zone)
            {
                case Zone.Ram: return "RAM";
                case Zone.Bus: return "BUS";
                case Zone.Cores: return "CPU CORES";
                default: return "ALU";
            }
        }

        /// <summary>Page heading: the chapter title and zone name joined by two spaces, or just the name without a title.</summary>
        public static string Heading(string chapterTitle, Zone zone)
        {
            string name = ZoneName(zone);
            return string.IsNullOrEmpty(chapterTitle) ? name : chapterTitle + "  " + name;
        }

        /// <summary>Second heading line naming the part and its job count.</summary>
        public static string Subline(Zone zone, int jobs)
        {
            switch (zone)
            {
                case Zone.Ram: return $"MEMORY BANK A - {jobs} JOBS";
                case Zone.Bus: return $"DATA LANES D7-D0 - {jobs} JOBS";
                case Zone.Cores: return $"DIE 0 - 4C/4T - {jobs} JOBS";
                // The robots work in the registers, which feed the ALU drawn beside them; the two are separate parts.
                default: return $"REGISTER FILE > ALU - {jobs} JOBS";
            }
        }

        /// <summary>Label of slot 0-7 inside a zone, e.g. U1, D7, CORE1, R0.</summary>
        public static string SlotLabel(Zone zone, int slot)
        {
            switch (zone)
            {
                case Zone.Ram: return "U" + (slot + 1);
                case Zone.Bus: return "D" + (7 - slot);
                case Zone.Cores: return CoresLabels[slot];
                default: return "R" + slot;
            }
        }

        /// <summary>Locked from <paramref name="unlocked"/> on; below it Cleared with stars, else Open.</summary>
        public static SlotState StateOf(int stage, int unlocked, int stars)
        {
            if (stage >= unlocked)
            {
                return SlotState.Locked;
            }

            return stars > 0 ? SlotState.Cleared : SlotState.Open;
        }

        /// <summary>The register value shown for a slot: the lock target when Cleared and the stage has one, else -1.</summary>
        public static int RegisterValue(SlotState state, int firstLockTarget)
        {
            return state == SlotState.Cleared && firstLockTarget >= 0 ? firstLockTarget : -1;
        }
    }
}
