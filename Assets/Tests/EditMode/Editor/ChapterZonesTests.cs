using NUnit.Framework;
using ProjectShift.Stage;

namespace ProjectShift.Tests
{
    public class ChapterZonesTests
    {
        [Test]
        public void ZoneOf_FirstFourChapters_RamBusCoresAlu()
        {
            Assert.AreEqual(Zone.Ram, ChapterZones.ZoneOf(0));
            Assert.AreEqual(Zone.Bus, ChapterZones.ZoneOf(1));
            Assert.AreEqual(Zone.Cores, ChapterZones.ZoneOf(2));
            Assert.AreEqual(Zone.Alu, ChapterZones.ZoneOf(3));
        }

        [Test]
        public void ZoneOf_ChapterPastAlu_StaysAlu()
        {
            Assert.AreEqual(Zone.Alu, ChapterZones.ZoneOf(4));
        }

        [Test]
        public void Heading_TitleAndZone_JoinsWithTwoSpaces()
        {
            Assert.AreEqual("NIGHT 1  RAM", ChapterZones.Heading("NIGHT 1", Zone.Ram));
            Assert.AreEqual("RAM", ChapterZones.Heading("", Zone.Ram));
        }

        [Test]
        public void Subline_Bus_NamesLanesAndJobs()
        {
            Assert.AreEqual("DATA LANES D7-D0 - 6 JOBS", ChapterZones.Subline(Zone.Bus, 6));
        }

        [Test]
        public void Subline_CoresAndAlu_UseRealHardwareTerms()
        {
            Assert.AreEqual("DIE 0 - 4C/4T - 7 JOBS", ChapterZones.Subline(Zone.Cores, 7));
            Assert.AreEqual("REGISTER FILE > ALU - 8 JOBS", ChapterZones.Subline(Zone.Alu, 8));
        }

        [Test]
        public void SlotLabel_BusFirstSlot_IsD7()
        {
            Assert.AreEqual("D7", ChapterZones.SlotLabel(Zone.Bus, 0));
        }

        [Test]
        public void SlotLabel_Cores_RunsInwardFromImcToCores()
        {
            Assert.AreEqual("IMC", ChapterZones.SlotLabel(Zone.Cores, 0));
            Assert.AreEqual("L1", ChapterZones.SlotLabel(Zone.Cores, 3));
            Assert.AreEqual("CORE1", ChapterZones.SlotLabel(Zone.Cores, 4));
            Assert.AreEqual("CORE4", ChapterZones.SlotLabel(Zone.Cores, 7));
        }

        [Test]
        public void StateOf_StarsButNotUnlocked_IsLocked()
        {
            Assert.AreEqual(SlotState.Locked, ChapterZones.StateOf(5, 3, 2));
        }

        [Test]
        public void StateOf_OpenWithoutStars_IsOpen()
        {
            Assert.AreEqual(SlotState.Open, ChapterZones.StateOf(2, 3, 0));
            Assert.AreEqual(SlotState.Cleared, ChapterZones.StateOf(2, 3, 1));
        }

        [Test]
        public void RegisterValue_ClearedWithoutLock_IsNone()
        {
            Assert.AreEqual(-1, ChapterZones.RegisterValue(SlotState.Cleared, -1));
            Assert.AreEqual(-1, ChapterZones.RegisterValue(SlotState.Open, 48));
        }

        [Test]
        public void RegisterValue_ClearedGoalZero_IsZero()
        {
            Assert.AreEqual(0, ChapterZones.RegisterValue(SlotState.Cleared, 0));
            Assert.AreEqual(48, ChapterZones.RegisterValue(SlotState.Cleared, 48));
        }
    }
}
