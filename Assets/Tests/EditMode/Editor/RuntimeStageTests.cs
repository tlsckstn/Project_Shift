using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectShift.Board;
using ProjectShift.Custom;
using ProjectShift.Stage;
using UnityEditor;
using UnityEngine;

namespace ProjectShift.Tests
{
    /// <summary>StageData.Create: a stage built from a code must play exactly like the asset the code came from.</summary>
    public class RuntimeStageTests
    {
        private const int Columns = BoardState.Columns;

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

        // Code text in between, so the stage really comes from a code and not from the test's own data object.
        private StageData FromCode(StageData official)
        {
            string code = StageCode.Encode(StageCodeTests.FromStage(official));
            Assert.IsTrue(StageCode.TryParse(code, out StageCodeData data, out string error), error);
            return Create(data, official.Par);
        }

        private static void AssertSameState(BoardState expected, BoardState actual)
        {
            Assert.AreEqual(expected.RowCount, actual.RowCount, "row count");
            Assert.AreEqual(expected.RobotCount, actual.RobotCount, "robot count");
            Assert.AreEqual(expected.ShiftsLeft, actual.ShiftsLeft, "shifts left");
            for (int r = 0; r < expected.RowCount; r++)
            {
                Assert.AreEqual(expected.GetRow(r), actual.GetRow(r), "row " + r);
                Assert.AreEqual(expected.IsRowLocked(r), actual.IsRowLocked(r), "locked row " + r);
                for (int c = 0; c < Columns; c++)
                {
                    Assert.AreEqual(expected.IsFirewall(r, c), actual.IsFirewall(r, c), $"firewall {r},{c}");
                    Assert.AreEqual(expected.IsBugCell(r, c), actual.IsBugCell(r, c), $"bug {r},{c}");
                }
            }

            for (int i = 0; i < expected.RobotCount; i++)
            {
                Assert.AreEqual(expected.GetRobotRow(i), actual.GetRobotRow(i), "robot row " + i);
                Assert.AreEqual(expected.GetRobotCol(i), actual.GetRobotCol(i), "robot col " + i);
            }
        }

        [TestCaseSource(nameof(StagePaths))]
        public void OfficialStage_FromCode_SameStateRobotsAndExits(string path)
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>(path);

            StageData custom = FromCode(official);

            AssertSameState(official.CreateState(), custom.CreateState());
            Assert.AreEqual(official.RobotCount, custom.RobotCount);
            CollectionAssert.AreEqual(official.GetExits(), custom.GetExits());
            Assert.AreEqual(official.NumberLockCount, custom.NumberLockCount);
            Assert.AreEqual(official.RobotsCoverBits, custom.RobotsCoverBits);
        }

        [TestCaseSource(nameof(StagePaths))]
        public void OfficialStage_FromCode_SolvesToSamePar(string path)
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>(path);
            StageData custom = FromCode(official);

            // The solver walks every reachable state through CreateState and IsCleared, so equal results mean equal clear rules.
            SolveResult expected = StageSolver.Solve(official);
            SolveResult actual = StageSolver.Solve(custom);

            Assert.IsTrue(actual.Solved);
            Assert.AreEqual(expected.Par, actual.Par);
            Assert.AreEqual(expected.StatesVisited, actual.StatesVisited);
        }

        [TestCaseSource(nameof(StagePaths))]
        public void OfficialStage_OnValidateAndParserBothAccept(string path)
        {
            StageData official = AssetDatabase.LoadAssetAtPath<StageData>(path);
            var warnings = new List<string>();
            void Collect(string message, string stack, LogType type)
            {
                if (type != LogType.Log)
                {
                    warnings.Add(message);
                }
            }

            Application.logMessageReceived += Collect;
            try
            {
                typeof(StageData).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(official, null);
            }
            finally
            {
                Application.logMessageReceived -= Collect;
            }

            CollectionAssert.IsEmpty(warnings, "OnValidate");
            Assert.IsNull(StageCode.Validate(StageCodeTests.FromStage(official)), "parser");
        }

        [Test]
        public void EveryFeature_ReachesCreateStateAndIsCleared()
        {
            // Row 0 11110001, row 1 00011111, row 2 00000000 (value 0). Lock on row 2 needs 0; robots cover their cells.
            var data = new StageCodeData(new byte[] { 0xF1, 0x1F, 0x00 }, new[] { 0, 11 }, new[] { 7, 15 }, 7,
                new[] { 1 }, new[] { 2 }, new[] { 3 }, new[] { 2 }, new[] { 0 }, true);

            StageData stage = Create(data);
            BoardState state = stage.CreateState();

            Assert.AreEqual(7, state.ShiftsLeft);
            Assert.IsTrue(state.IsRowLocked(1));
            Assert.IsFalse(state.IsRowLocked(0));
            Assert.IsTrue(state.IsFirewall(0, 2));
            Assert.IsTrue(state.IsBugCell(0, 3));
            Assert.AreEqual(1, state.GetRobotRow(1));
            Assert.AreEqual(3, state.GetRobotCol(1));
            Assert.IsTrue(stage.RobotsCoverBits);
            Assert.AreEqual(1, stage.NumberLockCount);
            Assert.IsFalse(stage.IsCleared(state), "robots not on exits");

            state.SetRobot(0, 0, 7);
            state.SetRobot(1, 1, 7);
            Assert.IsTrue(stage.IsCleared(state), "both robots on exits, lock row reads 0");

            state.SetRow(2, 0x01);
            Assert.IsFalse(stage.IsCleared(state), "lock row reads 1, target 0");
        }

        [Test]
        public void CoverSwitch_ChangesLockedRowValue()
        {
            // One robot standing on row 0's only 1 cell: covered, the row reads 0; uncovered, 128.
            var covered = new StageCodeData(new byte[] { 0x80 }, new[] { 0 }, new[] { 0 }, 1, null, null, null, new[] { 0 }, new[] { 0 }, true);
            var uncovered = new StageCodeData(new byte[] { 0x80 }, new[] { 0 }, new[] { 0 }, 1, null, null, null, new[] { 0 }, new[] { 0 }, false);

            StageData coveredStage = Create(covered);
            StageData uncoveredStage = Create(uncovered);

            Assert.IsTrue(coveredStage.IsCleared(coveredStage.CreateState()));
            Assert.IsFalse(uncoveredStage.IsCleared(uncoveredStage.CreateState()));
        }

        [Test]
        public void NoPar_HasNoParAndNoStars()
        {
            StageData stage = Create(new StageCodeData(new byte[] { 0x80 }, new[] { 0 }, new[] { 7 }, 3, null, null, null, null, null, false));

            Assert.AreEqual(0, stage.Par);
            Assert.AreEqual(0, stage.StarsFor(1));
        }

        [Test]
        public void GivenPar_StarsFollowIt()
        {
            StageData stage = Create(new StageCodeData(new byte[] { 0x80 }, new[] { 0 }, new[] { 7 }, 3, null, null, null, null, null, false), 2);

            Assert.AreEqual(2, stage.Par);
            Assert.AreEqual(3, stage.StarsFor(2));
            Assert.AreEqual(2, stage.StarsFor(3));
        }

        [Test]
        public void InvalidData_NotCreated()
        {
            // Robot starts on a 0 cell.
            var data = new StageCodeData(new byte[] { 0x00 }, new[] { 0 }, new[] { 7 }, 3, null, null, null, null, null, false);

            StageData stage = StageData.Create(data, 0, out string error);

            Assert.IsNull(stage);
            StringAssert.Contains("0 cell", error);
        }

        [Test]
        public void CreatedStage_NotOnDiskAndDestroyable()
        {
            StageData stage = StageData.Create(new StageCodeData(new byte[] { 0x80 }, new[] { 0 }, new[] { 7 }, 3, null, null, null, null, null, false), 0, out _);

            Assert.IsFalse(EditorUtility.IsPersistent(stage));
            Assert.IsEmpty(AssetDatabase.GetAssetPath(stage));
            Assert.IsEmpty(stage.Hint ?? "");
            Assert.IsEmpty(stage.ChapterTitle ?? "");

            Object.DestroyImmediate(stage);

            Assert.IsTrue(stage == null);
        }
    }
}
