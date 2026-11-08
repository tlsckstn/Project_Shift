using System.Collections.Generic;
using NUnit.Framework;
using ProjectShift.Stage;

namespace ProjectShift.Tests
{
    public class StagePagesTests
    {
        private const int PageSize = 8;

        private readonly List<int> starts = new List<int>();
        private readonly List<string> titles = new List<string>();
        private readonly List<int> chapters = new List<int>();

        [Test]
        public void Build_TwoChapters_StartsPageAtEachTitle()
        {
            StagePages.Build(new[] { "NIGHT 1", "", "", "NIGHT 2", "" }, PageSize, starts, titles, chapters);

            CollectionAssert.AreEqual(new[] { 0, 3 }, starts);
            CollectionAssert.AreEqual(new[] { "NIGHT 1", "NIGHT 2" }, titles);
            CollectionAssert.AreEqual(new[] { 0, 1 }, chapters);
        }

        [Test]
        public void Build_LongChapter_SplitsAtPageSizeKeepingChapter()
        {
            string[] tenStages = new string[10];
            tenStages[0] = "NIGHT 1";

            StagePages.Build(tenStages, PageSize, starts, titles, chapters);

            CollectionAssert.AreEqual(new[] { 0, 8 }, starts);
            CollectionAssert.AreEqual(new[] { "NIGHT 1", "NIGHT 1" }, titles);
            CollectionAssert.AreEqual(new[] { 0, 0 }, chapters);
        }

        [Test]
        public void Build_ChapterExactlyPageSize_NoEmptyPage()
        {
            string[] stages = new string[9];
            stages[0] = "NIGHT 1";
            stages[8] = "NIGHT 2";

            StagePages.Build(stages, PageSize, starts, titles, chapters);

            CollectionAssert.AreEqual(new[] { 0, 8 }, starts);
            CollectionAssert.AreEqual(new[] { "NIGHT 1", "NIGHT 2" }, titles);
            CollectionAssert.AreEqual(new[] { 0, 1 }, chapters);
        }

        [Test]
        public void PageOf_ExactPageStart_ReturnsThatPage()
        {
            Assert.AreEqual(1, StagePages.PageOf(new List<int> { 0, 8 }, 8));
        }

        [Test]
        public void PageOf_StageOnSecondPage_ReturnsOne()
        {
            Assert.AreEqual(1, StagePages.PageOf(new List<int> { 0, 8 }, 9));
        }

        [Test]
        public void NextSelection_PastLastOpen_Stays()
        {
            Assert.AreEqual(7, StagePages.NextSelection(7, 1, 8));
        }

        [Test]
        public void NextSelection_BeforeFirst_Stays()
        {
            Assert.AreEqual(0, StagePages.NextSelection(0, -1, 8));
        }

        [Test]
        public void NextSelection_AcrossPageEdge_Moves()
        {
            Assert.AreEqual(8, StagePages.NextSelection(7, 1, 20));
        }
    }
}
