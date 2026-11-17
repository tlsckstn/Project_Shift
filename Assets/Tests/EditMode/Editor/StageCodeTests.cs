using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectShift.Custom;
using ProjectShift.Stage;
using UnityEditor;
using UnityEngine;

namespace ProjectShift.Tests
{
    /// <summary>Stage code round trip over every shipped stage, one case per rejection rule, and garbage input that must not throw.</summary>
    public class StageCodeTests
    {
        private const int Columns = 8;

        internal static IEnumerable<string> StagePaths()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:StageData", new[] { "Assets/Data/Stages" }))
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        // 2 rows, robot 0 on (0,0), exit (1,7); every list empty.
        private static StageCodeData Valid(byte[] rows = null, int[] starts = null, int[] exits = null, int shiftLimit = 5,
            int[] lockedRows = null, int[] firewalls = null, int[] bugCells = null, int[] lockRows = null, int[] lockTargets = null)
        {
            return new StageCodeData(rows ?? new byte[] { 0b1111_0000, 0b0000_1111 }, starts ?? new[] { 0 }, exits ?? new[] { 15 },
                shiftLimit, lockedRows, firewalls, bugCells, lockRows, lockTargets, false);
        }

        private static string Reject(StageCodeData data)
        {
            Assert.IsFalse(StageCode.TryParse(StageCode.Encode(data), out StageCodeData parsed, out string error));
            Assert.IsNull(parsed);
            Assert.IsNotEmpty(error);
            return error;
        }

        private static string RejectText(string code)
        {
            Assert.IsFalse(StageCode.TryParse(code, out StageCodeData parsed, out string error));
            Assert.IsNull(parsed);
            Assert.IsNotEmpty(error);
            return error;
        }

        private static StageCodeData RoundTrip(StageCodeData data)
        {
            string code = StageCode.Encode(data);
            Assert.IsTrue(StageCode.TryParse(code, out StageCodeData parsed, out string error), error);
            Assert.That(code, Does.Match("^[A-Za-z0-9_-]+$"));
            return parsed;
        }

        private static int Cell(Vector2Int cell)
        {
            return cell.x * Columns + cell.y;
        }

        private static int[] Cells(SerializedProperty list)
        {
            int[] cells = new int[list.arraySize];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = Cell(list.GetArrayElementAtIndex(i).vector2IntValue);
            }

            return cells;
        }

        // Reads the private fields the way the inspector stores them. Test-only: the game goes code -> StageData, never back.
        internal static StageCodeData FromStage(StageData stage)
        {
            var serialized = new SerializedObject(stage);
            SerializedProperty rowList = serialized.FindProperty("rows");
            byte[] rows = new byte[rowList.arraySize];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = Convert.ToByte(rowList.GetArrayElementAtIndex(i).stringValue, 2);
            }

            var starts = new List<int> { Cell(serialized.FindProperty("start").vector2IntValue) };
            starts.AddRange(Cells(serialized.FindProperty("extraStarts")));
            var exits = new List<int> { Cell(serialized.FindProperty("exit").vector2IntValue) };
            exits.AddRange(Cells(serialized.FindProperty("extraExits")));

            var lockedRows = new SortedSet<int>();
            SerializedProperty lockedList = serialized.FindProperty("lockedRows");
            for (int i = 0; i < lockedList.arraySize; i++)
            {
                lockedRows.Add(lockedList.GetArrayElementAtIndex(i).intValue);
            }

            SerializedProperty locks = serialized.FindProperty("numberLocks");
            int[] lockRows = new int[locks.arraySize];
            int[] lockTargets = new int[locks.arraySize];
            for (int i = 0; i < lockRows.Length; i++)
            {
                Vector2Int numberLock = locks.GetArrayElementAtIndex(i).vector2IntValue;
                lockRows[i] = numberLock.x;
                lockTargets[i] = numberLock.y;
            }

            return new StageCodeData(rows, starts.ToArray(), exits.ToArray(), serialized.FindProperty("shiftLimit").intValue,
                new List<int>(lockedRows).ToArray(), Cells(serialized.FindProperty("firewalls")), Cells(serialized.FindProperty("bugCells")),
                lockRows, lockTargets, serialized.FindProperty("robotsCoverBits").boolValue);
        }

        private static void AssertSame(StageCodeData expected, StageCodeData actual)
        {
            CollectionAssert.AreEqual(expected.Rows, actual.Rows, "rows");
            CollectionAssert.AreEqual(expected.Starts, actual.Starts, "starts");
            CollectionAssert.AreEqual(expected.Exits, actual.Exits, "exits");
            Assert.AreEqual(expected.ShiftLimit, actual.ShiftLimit, "shift limit");
            CollectionAssert.AreEqual(expected.LockedRows, actual.LockedRows, "locked rows");
            CollectionAssert.AreEqual(expected.Firewalls, actual.Firewalls, "firewalls");
            CollectionAssert.AreEqual(expected.BugCells, actual.BugCells, "bug cells");
            CollectionAssert.AreEqual(expected.LockRows, actual.LockRows, "lock rows");
            CollectionAssert.AreEqual(expected.LockTargets, actual.LockTargets, "lock targets");
            Assert.AreEqual(expected.RobotsCoverBits, actual.RobotsCoverBits, "robots cover bits");
        }

        [Test]
        public void StageFolder_Has32Stages()
        {
            Assert.AreEqual(32, new List<string>(StagePaths()).Count);
        }

        [TestCaseSource(nameof(StagePaths))]
        public void OfficialStage_RoundTripsUnchanged(string path)
        {
            StageCodeData original = FromStage(AssetDatabase.LoadAssetAtPath<StageData>(path));

            StageCodeData parsed = RoundTrip(original);

            AssertSame(original, parsed);
        }

        [Test]
        public void AllFeatures_RoundTripUnchanged()
        {
            var original = new StageCodeData(new byte[] { 0xFF, 0x81, 0x3C }, new[] { 0, 7, 16 + 2 }, new[] { 23, 8, 1 }, StageCode.MaxShiftLimit,
                new[] { 0, 2 }, new[] { 3, 4 }, new[] { 9, 22 }, new[] { 1, 2 }, new[] { 0, 255 }, true);

            AssertSame(original, RoundTrip(original));
        }

        [Test]
        public void OneRowOneRobot_RoundTrips()
        {
            StageCodeData original = Valid(rows: new byte[] { 0x80 }, starts: new[] { 0 }, exits: new[] { 7 }, shiftLimit: 0);

            AssertSame(original, RoundTrip(original));
        }

        [Test]
        public void MaxRowsThreeRobots_RoundTrips()
        {
            StageCodeData original = Valid(rows: new byte[] { 0x80, 0x80, 0x80 }, starts: new[] { 0, 8, 16 }, exits: new[] { 7, 15, 23 });

            AssertSame(original, RoundTrip(original));
        }

        [Test]
        public void SurroundingWhitespace_IsTrimmed()
        {
            StageCodeData original = Valid();

            Assert.IsTrue(StageCode.TryParse("  \r\n\t" + StageCode.Encode(original) + " \n", out StageCodeData parsed, out string error), error);
            AssertSame(original, parsed);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   \n")]
        public void EmptyOrNull_Rejected(string code)
        {
            StringAssert.Contains("empty", RejectText(code));
        }

        [TestCase("AQ+B")]
        [TestCase("AQ/B")]
        [TestCase("AQ==")]
        [TestCase("AQ B")]
        [TestCase("한글")]
        public void DisallowedCharacter_Rejected(string code)
        {
            StringAssert.Contains("not allowed", RejectText(code));
        }

        [Test]
        public void ImpossibleBase64Length_Rejected()
        {
            // Base64 text whose length is 1 mod 4 cannot be decoded.
            StringAssert.Contains("wrong length", RejectText("AQAAA"));
        }

        [Test]
        public void TooLong_Rejected()
        {
            StringAssert.Contains("longer than", RejectText(new string('A', StageCode.MaxCodeLength + 1)));
        }

        [Test]
        public void CutOffCode_Rejected()
        {
            string code = StageCode.Encode(Valid());

            RejectText(code.Substring(0, code.Length - 2));
        }

        [Test]
        public void ExtraDataAtEnd_Rejected()
        {
            string base64 = StageCode.Encode(Valid()).Replace('-', '+').Replace('_', '/');
            byte[] bytes = Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
            Array.Resize(ref bytes, bytes.Length + 1);
            string code = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

            StringAssert.Contains("extra data", RejectText(code));
        }

        [Test]
        public void UnknownVersion_Rejected()
        {
            StringAssert.Contains("version", RejectText("AgA"));
        }

        [Test]
        public void ZeroRows_Rejected()
        {
            StringAssert.Contains("rows", Reject(Valid(rows: new byte[0])));
        }

        [Test]
        public void TooManyRows_RejectedByValidate()
        {
            // Four rows do not fit the 2-bit header field, so this rule is reachable only from data an editor builds.
            StringAssert.Contains("rows", StageCode.Validate(Valid(rows: new byte[] { 0x80, 0x80, 0x80, 0x80 })));
        }

        [Test]
        public void ZeroRobots_Rejected()
        {
            StringAssert.Contains("robots", Reject(Valid(starts: new int[0], exits: new int[0])));
        }

        [Test]
        public void RobotAndExitCountsDiffer_Rejected()
        {
            StringAssert.Contains("exits", Reject(Valid(exits: new[] { 15, 14 })));
        }

        [Test]
        public void RobotStartOffBoard_Rejected()
        {
            StringAssert.Contains("Robot start", Reject(Valid(starts: new[] { 2 * Columns })));
        }

        [Test]
        public void ExitOffBoard_Rejected()
        {
            StringAssert.Contains("Exit", Reject(Valid(exits: new[] { 2 * Columns })));
        }

        [Test]
        public void FirewallOffBoard_Rejected()
        {
            StringAssert.Contains("Firewall", Reject(Valid(firewalls: new[] { 2 * Columns })));
        }

        [Test]
        public void BugCellOffBoard_Rejected()
        {
            StringAssert.Contains("Bug cell", Reject(Valid(bugCells: new[] { 2 * Columns })));
        }

        [Test]
        public void RobotStartOnZeroCell_Rejected()
        {
            StringAssert.Contains("0 cell", Reject(Valid(starts: new[] { 4 })));
        }

        [Test]
        public void TwoRobotsSameStart_Rejected()
        {
            StringAssert.Contains("same cell", Reject(Valid(starts: new[] { 0, 0 }, exits: new[] { 15, 14 })));
        }

        [Test]
        public void NumberLockRowOffBoard_Rejected()
        {
            StringAssert.Contains("Number lock row", Reject(Valid(lockRows: new[] { 2 }, lockTargets: new[] { 1 })));
        }

        [Test]
        public void TwoNumberLocksOnOneRow_Rejected()
        {
            StringAssert.Contains("two number locks", Reject(Valid(lockRows: new[] { 1, 1 }, lockTargets: new[] { 1, 2 })));
        }

        [TestCase(-1)]
        [TestCase(256)]
        public void NumberLockTargetOutOfRange_RejectedByValidate(int target)
        {
            // A target is one byte in the code, so out-of-range values come only from editor-built data.
            StringAssert.Contains("0-255", StageCode.Validate(Valid(lockRows: new[] { 0 }, lockTargets: new[] { target })));
        }

        [Test]
        public void LockedRowOffBoard_Rejected()
        {
            StringAssert.Contains("Locked row", Reject(Valid(lockedRows: new[] { 2 })));
        }

        [Test]
        public void ShiftLimitAboveMax_Rejected()
        {
            StringAssert.Contains("Shift limit", Reject(Valid(shiftLimit: StageCode.MaxShiftLimit + 1)));
        }

        [Test]
        public void NegativeShiftLimit_RejectedByValidate()
        {
            StringAssert.Contains("Shift limit", StageCode.Validate(Valid(shiftLimit: -1)));
        }

        [Test]
        public void ShiftLimitAtMax_Accepted()
        {
            Assert.IsNull(StageCode.Validate(Valid(shiftLimit: StageCode.MaxShiftLimit)));
        }

        [Test]
        public void EveryPrefixAndOneCharChange_NeverThrows()
        {
            var full = new StageCodeData(new byte[] { 0xFF, 0x81, 0x3C }, new[] { 0, 7, 18 }, new[] { 23, 8, 1 }, 9,
                new[] { 0 }, new[] { 3 }, new[] { 9 }, new[] { 1 }, new[] { 129 }, true);
            string code = StageCode.Encode(full);
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

            for (int length = 0; length <= code.Length; length++)
            {
                Assert.DoesNotThrow(() => StageCode.TryParse(code.Substring(0, length), out _, out _));
            }

            for (int i = 0; i < code.Length; i++)
            {
                foreach (char c in alphabet)
                {
                    string changed = code.Substring(0, i) + c + code.Substring(i + 1);
                    Assert.DoesNotThrow(() => StageCode.TryParse(changed, out _, out _), changed);
                }
            }
        }

        [Test]
        public void RandomText_NeverThrowsAndRejectsWithReason()
        {
            // Fixed seed: the same 2000 strings every run.
            var random = new System.Random(20261117);
            for (int n = 0; n < 2000; n++)
            {
                char[] text = new char[random.Next(0, 200)];
                for (int i = 0; i < text.Length; i++)
                {
                    text[i] = (char)random.Next(0, 0x3000);
                }

                string code = new string(text);
                bool ok = true;
                string error = null;
                Assert.DoesNotThrow(() => ok = StageCode.TryParse(code, out _, out error));
                Assert.IsTrue(ok || !string.IsNullOrEmpty(error));
            }
        }
    }
}
