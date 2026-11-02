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

        /// <summary>Exit cell as (row, column).</summary>
        public Vector2Int Exit => exit;

        /// <summary>Fewest shifts that clear the stage; 0 means not set and is not shown.</summary>
        public int Par => par;

        /// <summary>Builds a fresh board state from the row strings, start position and shift limit.</summary>
        public BoardState CreateState()
        {
            var bytes = new byte[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                bytes[i] = Convert.ToByte(rows[i], 2);
            }

            return new BoardState(bytes, start.x, start.y, shiftLimit);
        }
    }
}
