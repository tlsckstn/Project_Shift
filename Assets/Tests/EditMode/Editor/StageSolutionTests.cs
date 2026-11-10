using System.Collections.Generic;
using NUnit.Framework;
using ProjectShift.Editor;
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
    }
}
