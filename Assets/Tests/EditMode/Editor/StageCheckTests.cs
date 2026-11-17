using System.Collections.Generic;
using NUnit.Framework;
using ProjectShift.Custom;
using ProjectShift.Stage;
using UnityEditor;
using UnityEngine;

namespace ProjectShift.Tests
{
    /// <summary>StageCheck runs the solver a slice per frame; these drive it to the end and check every outcome.</summary>
    public class StageCheckTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object stage in created)
            {
                Object.DestroyImmediate(stage);
            }

            created.Clear();
        }

        private static IEnumerable<string> StagePaths()
        {
            return StageCodeTests.StagePaths();
        }

        private StageData Create(StageCodeData data, int par = 0)
        {
            StageData stage = StageData.Create(data, par, out string error);
            Assert.IsNotNull(stage, error);
            created.Add(stage);
            return stage;
        }

        private static StageCodeData WithLimit(StageCodeData data, int shiftLimit)
        {
            return new StageCodeData(data.Rows, data.Starts, data.Exits, shiftLimit, data.LockedRows, data.Firewalls,
                data.BugCells, data.LockRows, data.LockTargets, data.RobotsCoverBits);
        }

        // Three rows of 1s, three robots, exits all on cell 0: two robots cannot share a cell, so it never clears
        // and the search has to walk every state. Measured: over 1,000,000 states.
        private static StageCodeData Endless()
        {
            return new StageCodeData(new byte[] { 0xFF, 0xFF, 0xFF }, new[] { 0, 8, 16 }, new[] { 0, 0, 0 }, 99,
                null, null, null, null, null, false);
        }

        private static void RunToEnd(StageCheck check)
        {
            // Safety bound only: a Tick also spends its budget on queued states it has already seen.
            for (int i = 0; i < 100000 && check.Status == StageCheckStatus.Running; i++)
            {
                check.Tick();
            }
        }

        [Test]
        public void NewCheck_IsIdle()
        {
            var check = new StageCheck();

            Assert.AreEqual(StageCheckStatus.Idle, check.Status);
            Assert.AreEqual(-1, check.Par);
            Assert.AreEqual(0, check.StatesVisited);
        }

        [TestCaseSource(nameof(StagePaths))]
        public void OfficialStage_SlicedCheck_MatchesSolve(string path)
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>(path);
            var check = new StageCheck();

            check.Start(official);
            RunToEnd(check);

            SolveResult whole = StageSolver.Solve(official);
            Assert.AreEqual(StageCheckStatus.Solved, check.Status);
            Assert.AreEqual(whole.Par, check.Par);
            Assert.AreEqual(whole.StatesVisited, check.StatesVisited);
        }

        [Test]
        public void SolverCap_ReturnsTooComplexNotUnsolvable()
        {
            StageData stage = Create(Endless());

            var solver = new StageSolver(stage, 500);
            solver.Step(int.MaxValue);

            Assert.IsTrue(solver.Result.TooComplex);
            Assert.IsFalse(solver.Result.Solved);
            Assert.AreEqual(500, solver.Result.StatesVisited);
        }

        [Test]
        public void EndlessBoard_CheckStopsAtCapAsTooComplex()
        {
            var check = new StageCheck();

            check.Start(Create(Endless()));
            RunToEnd(check);

            Assert.AreEqual(StageCheckStatus.TooComplex, check.Status);
            Assert.AreEqual(StageCheck.StateCap, check.StatesVisited);
            Assert.AreEqual(-1, check.Par);
        }

        [Test]
        public void Running_ProgressGrowsEachTick()
        {
            var check = new StageCheck();
            check.Start(Create(Endless()));

            check.Tick();
            int first = check.StatesVisited;
            check.Tick();

            Assert.AreEqual(StageCheckStatus.Running, check.Status);
            Assert.That(first, Is.InRange(1, StageCheck.StatesPerFrame));
            Assert.Greater(check.StatesVisited, first);
        }

        [Test]
        public void RestartMidRun_ReportsOnlyTheNewBoard()
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage26.asset");
            int par = StageSolver.Solve(official).Par;
            var check = new StageCheck();
            check.Start(Create(Endless()));
            check.Tick();
            check.Tick();

            check.Start(official);
            RunToEnd(check);

            Assert.AreEqual(StageCheckStatus.Solved, check.Status);
            Assert.AreEqual(par, check.Par);
        }

        [Test]
        public void Cancel_ReturnsToIdle()
        {
            var check = new StageCheck();
            check.Start(Create(Endless()));
            check.Tick();

            check.Cancel();
            check.Tick();

            Assert.AreEqual(StageCheckStatus.Idle, check.Status);
            Assert.AreEqual(0, check.StatesVisited);
        }

        [Test]
        public void LimitBelowPar_Unsolvable_AndRaisingItSolves()
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage26.asset");
            StageCodeData data = StageCodeTests.FromStage(official);
            int par = StageSolver.Solve(official).Par;
            var check = new StageCheck();

            check.Start(Create(WithLimit(data, par - 1)));
            RunToEnd(check);
            Assert.AreEqual(StageCheckStatus.Unsolvable, check.Status);
            Assert.AreEqual(-1, check.Par);

            check.Start(Create(WithLimit(data, par)));
            RunToEnd(check);
            Assert.AreEqual(StageCheckStatus.Solved, check.Status);
            Assert.AreEqual(par, check.Par);
        }

        [Test]
        public void SolvedPar_GoesIntoStageData()
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage26.asset");
            StageCodeData data = StageCodeTests.FromStage(official);
            var check = new StageCheck();
            check.Start(Create(data));
            RunToEnd(check);

            StageData withPar = Create(data, check.Par);

            Assert.AreEqual(official.Par, withPar.Par);
            Assert.AreEqual(3, withPar.StarsFor(check.Par));
        }
    }
}
