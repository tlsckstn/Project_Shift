using System;
using ProjectShift.Board;
using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>
    /// One stage, typed in the inspector: row strings, robot start, exit and shift limit.
    /// Coordinates are (row, column): x is the row from the top, y is the cell from the left.
    /// Robot 0 uses Start and Exit; a stage with more robots lists them in Extra Starts and Extra Exits.
    /// </summary>
    [CreateAssetMenu(fileName = "Stage", menuName = "Project Shift/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Tooltip("One 8-character binary string per row, top row first. Cell 0 is the leftmost character, e.g. \"00000110\".")]
        [SerializeField] private string[] rows = { "00000000" };
        [Tooltip("x = row, y = column")]
        [SerializeField] private Vector2Int start;
        [Tooltip("x = row, y = column")]
        [SerializeField] private Vector2Int exit;
        [Tooltip("Start of each extra robot (up to 2), x = row, y = column. Empty means one robot. Must have the same length as Extra Exits.")]
        [SerializeField] private Vector2Int[] extraStarts;
        [Tooltip("Exit of each extra robot, in the same order as Extra Starts.")]
        [SerializeField] private Vector2Int[] extraExits;
        [SerializeField] private int shiftLimit;
        [Tooltip("Fewest shifts that clear the stage (verified by BFS). 0 hides PAR.")]
        [SerializeField] private int par;
        [Tooltip("Optional one-line hint in English. Explains rules only, never keys. Empty hides it.")]
        [SerializeField] private string hint;
        [Tooltip("Row indices (0 = top) that cannot be shifted.")]
        [SerializeField] private int[] lockedRows;
        [Tooltip("Firewall cells fixed to the board, x = row, y = column. Never place one on the exit.")]
        [SerializeField] private Vector2Int[] firewalls;
        [Tooltip("Bug cells fixed to the board, x = row, y = column. Entering one by any move fails the stage. Never place one on the start or exit.")]
        [SerializeField] private Vector2Int[] bugCells;
        [Tooltip("Number locks, x = row (0 = top), y = target decimal value 0-255. The stage clears only while every robot stands on its exit AND each locked row reads its target. At most one lock per row.")]
        [SerializeField] private Vector2Int[] numberLocks;
        [Tooltip("When on, a robot covers the cell it stands on: that cell counts as 0 in the row's decimal value (shown value and number locks). Terrain is unchanged. Off by default.")]
        [SerializeField] private bool robotsCoverBits;

        /// <summary>Number of robots: robot 0 plus the extra robots.</summary>
        public int RobotCount => 1 + (extraStarts == null ? 0 : extraStarts.Length);

        /// <summary>Exit cell of robot <paramref name="robot"/> as (row, column).</summary>
        public Vector2Int GetExit(int robot)
        {
            return robot == 0 ? exit : extraExits[robot - 1];
        }

        /// <summary>Exit cells of every robot, in robot order. Builds a new array: call it when a stage loads, not every frame.</summary>
        public Vector2Int[] GetExits()
        {
            var exits = new Vector2Int[RobotCount];
            for (int i = 0; i < exits.Length; i++)
            {
                exits[i] = GetExit(i);
            }

            return exits;
        }

        /// <summary>Number of number locks; 0 means a plain stage.</summary>
        public int NumberLockCount => numberLocks == null ? 0 : numberLocks.Length;

        /// <summary>Number lock <paramref name="index"/> as (row, target decimal value).</summary>
        public Vector2Int GetNumberLock(int index)
        {
            return numberLocks[index];
        }

        /// <summary>True when robots cover the cell they stand on (counted as 0) in row values.</summary>
        public bool RobotsCoverBits => robotsCoverBits;

        /// <summary>True when the locked row of number lock <paramref name="index"/> reads its target value right now.</summary>
        public bool IsNumberLockMet(BoardState state, int index)
        {
            Vector2Int numberLock = numberLocks[index];
            return BoardLogic.GetRowValue(state, numberLock.x, robotsCoverBits) == numberLock.y;
        }

        /// <summary>True when every robot stands on its own exit and every number lock is met at this moment.</summary>
        public bool IsCleared(BoardState state)
        {
            for (int i = 0; i < state.RobotCount; i++)
            {
                Vector2Int robotExit = GetExit(i);
                if (state.GetRobotRow(i) != robotExit.x || state.GetRobotCol(i) != robotExit.y)
                {
                    return false;
                }
            }

            for (int i = 0; i < NumberLockCount; i++)
            {
                if (!IsNumberLockMet(state, i))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Fewest shifts that clear the stage; 0 means not set and is not shown.</summary>
        public int Par => par;

        /// <summary>Shifts the stage starts with.</summary>
        public int ShiftLimit => shiftLimit;

        /// <summary>One-line rule hint shown during the stage; empty or null means no hint.</summary>
        public string Hint => hint;

        /// <summary>Builds a fresh board state from the row strings, robot starts, shift limit, locked rows, firewalls and bug cells.</summary>
        public BoardState CreateState()
        {
            var bytes = new byte[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                bytes[i] = Convert.ToByte(rows[i], 2);
            }

            bool[] locked = null;
            if (lockedRows != null && lockedRows.Length > 0)
            {
                locked = new bool[rows.Length];
                foreach (int row in lockedRows)
                {
                    locked[row] = true;
                }
            }

            int robotCount = RobotCount;
            var robotRows = new int[robotCount];
            var robotCols = new int[robotCount];
            robotRows[0] = start.x;
            robotCols[0] = start.y;
            for (int i = 1; i < robotCount; i++)
            {
                robotRows[i] = extraStarts[i - 1].x;
                robotCols[i] = extraStarts[i - 1].y;
            }

            return new BoardState(bytes, robotRows, robotCols, shiftLimit, locked, ToCellFlags(firewalls), ToCellFlags(bugCells));
        }

        // Data-file boundary: catch a mistyped robot list as soon as the asset is edited.
        private void OnValidate()
        {
            int starts = extraStarts == null ? 0 : extraStarts.Length;
            int exits = extraExits == null ? 0 : extraExits.Length;
            if (starts != exits || starts > 2)
            {
                Debug.LogWarning($"{name}: Extra Starts ({starts}) and Extra Exits ({exits}) must have the same length, at most 2.", this);
            }

            for (int i = 0; i < NumberLockCount; i++)
            {
                Vector2Int numberLock = numberLocks[i];
                bool duplicate = false;
                for (int j = 0; j < i; j++)
                {
                    duplicate |= numberLocks[j].x == numberLock.x;
                }

                if (numberLock.x < 0 || numberLock.x >= rows.Length || numberLock.y < 0 || numberLock.y > 255 || duplicate)
                {
                    Debug.LogWarning($"{name}: Number Lock {i} (row {numberLock.x}, target {numberLock.y}) needs a row inside the board, a target 0-255 and at most one lock per row.", this);
                }
            }
        }

        // One flag per board cell (row * Columns + column); null when the list is empty.
        private bool[] ToCellFlags(Vector2Int[] cells)
        {
            if (cells == null || cells.Length == 0)
            {
                return null;
            }

            var flags = new bool[rows.Length * BoardState.Columns];
            foreach (Vector2Int cell in cells)
            {
                flags[cell.x * BoardState.Columns + cell.y] = true;
            }

            return flags;
        }
    }
}
