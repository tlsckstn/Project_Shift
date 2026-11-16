using System.Collections.Generic;
using NUnit.Framework;
using ProjectShift.Stage;
using UnityEditor;

namespace ProjectShift.Tests
{
    /// <summary>Re-solves every shipped stage so a board edit that breaks its par or limit fails here, not in the jam build.</summary>
    public class StageSolutionTests
    {
        private static IEnumerable<string> StagePaths()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:StageData", new[] { "Assets/Data/Stages" }))
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        [Test]
        public void StageFolder_HasStages()
        {
            Assert.IsNotEmpty(new List<string>(StagePaths()));
        }

        [TestCaseSource(nameof(StagePaths))]
        public void Stage_SolverMatchesParAndLimit(string path)
        {
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);

            SolveResult result = StageSolver.Solve(stage);

            Assert.IsTrue(result.Solved, "unsolvable within limit " + stage.ShiftLimit);
            Assert.AreEqual(stage.Par, result.Par, "par; solution: " + string.Join(", ", result.Actions));
            // Brief: limit = par + 1-2 spare shifts.
            int spare = stage.ShiftLimit - stage.Par;
            Assert.That(spare, Is.InRange(1, 2), "spare shifts over par");
        }

        [TestCaseSource(nameof(StagePaths))]
        public void Stage_SlicedStepsMatchSolve(string path)
        {
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            SolveResult whole = StageSolver.Solve(stage);
            var solver = new StageSolver(stage);

            // One state per slice: every level boundary falls between two calls.
            while (!solver.Step(1))
            {
            }

            Assert.AreEqual(whole.Par, solver.Result.Par);
            Assert.AreEqual(whole.StatesVisited, solver.Result.StatesVisited);
            Assert.AreEqual(string.Join(", ", whole.Actions), string.Join(", ", solver.Result.Actions));
        }
    }
}
