using System;
using ProjectShift.Board;
using UnityEngine;

namespace ProjectShift.Stage
{
    /// <summary>
    /// One stage, typed in the inspector: row strings, robot start, exit and shift limit.
    /// Coordinates are (row, column): x is the row from the top, y is the cell from the left.
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
        [SerializeField] private int shiftLimit;
        [Tooltip("Fewest shifts that clear the stage (verified by BFS). 0 hides PAR.")]
        [SerializeField] private int par;
        [Tooltip("Optional one-line hint in English. Explains rules only, never keys. Empty hides it.")]
        [SerializeField] private string hint;
        [Tooltip("Row indices (0 = top) that cannot be shifted.")]
        [SerializeField] private int[] lockedRows;
        [Tooltip("Firewall cells fixed to the board, x = row, y = column. Never place one on the exit.")]
        [SerializeField] private Vector2Int[] firewalls;

        /// <summary>Exit cell as (row, column).</summary>
        public Vector2Int Exit => exit;

        /// <summary>Fewest shifts that clear the stage; 0 means not set and is not shown.</summary>
        public int Par => par;

        /// <summary>Shifts the stage starts with.</summary>
        public int ShiftLimit => shiftLimit;

        /// <summary>One-line rule hint shown during the stage; empty or null means no hint.</summary>
        public string Hint => hint;

        /// <summary>Builds a fresh board state from the row strings, start position, shift limit, locked rows and firewalls.</summary>
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

            bool[] walls = null;
            if (firewalls != null && firewalls.Length > 0)
            {
                walls = new bool[rows.Length * BoardState.Columns];
                foreach (Vector2Int cell in firewalls)
                {
                    walls[cell.x * BoardState.Columns + cell.y] = true;
                }
            }

            return new BoardState(bytes, start.x, start.y, shiftLimit, locked, walls);
        }
    }
}
