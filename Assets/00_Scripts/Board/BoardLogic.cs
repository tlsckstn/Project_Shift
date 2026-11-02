namespace ProjectShift.Board
{
    /// <summary>
    /// Pure-C# board rules (no MonoBehaviour, callable from Edit Mode tests):
    /// row shifts, robot walking and descending, and falling off a row end.
    /// Shift counting lives elsewhere.
    /// </summary>
    public static class BoardLogic
    {
        /// <summary>
        /// Shifts a row one cell left (cell 1 moves to cell 0). The right end becomes 0 and the bit pushed past the left end is lost.
        /// A robot on that row moves left with it and falls if it leaves cell 0.
        /// </summary>
        /// <example>
        /// BoardLogic.ShiftLeft(state, 0); // 10000000 becomes 00000000
        /// </example>
        public static void ShiftLeft(BoardState state, int row)
        {
            state.SetRow(row, (byte)(state.GetRow(row) << 1));
            MoveRobotWithRow(state, row, -1);
        }

        /// <summary>
        /// Shifts a row one cell right (cell 0 moves to cell 1). The left end becomes 0 and the bit pushed past the right end is lost.
        /// A robot on that row moves right with it and falls if it leaves cell 7.
        /// </summary>
        /// <example>
        /// BoardLogic.ShiftRight(state, 0); // 00000001 becomes 00000000
        /// </example>
        public static void ShiftRight(BoardState state, int row)
        {
            state.SetRow(row, (byte)(state.GetRow(row) >> 1));
            MoveRobotWithRow(state, row, 1);
        }

        /// <summary>
        /// Walks the robot one cell along its row (<paramref name="direction"/> is -1 for left, 1 for right).
        /// Succeeds only when the target cell is on. Returns false and does nothing otherwise.
        /// </summary>
        public static bool TryWalk(BoardState state, int direction)
        {
            int col = state.RobotCol + direction;
            if (state.IsRobotFallen || col < 0 || col >= BoardState.Columns || !state.GetCell(state.RobotRow, col))
            {
                return false;
            }

            state.SetRobot(state.RobotRow, col);
            return true;
        }

        /// <summary>
        /// Moves the robot down one row when the cell directly below is on. There is no way up.
        /// Returns false and does nothing otherwise.
        /// </summary>
        public static bool TryDescend(BoardState state)
        {
            int row = state.RobotRow + 1;
            if (state.IsRobotFallen || row >= state.RowCount || !state.GetCell(row, state.RobotCol))
            {
                return false;
            }

            state.SetRobot(row, state.RobotCol);
            return true;
        }

        // The robot rides its row even when the cell under it was shifted away; it only falls past an end.
        private static void MoveRobotWithRow(BoardState state, int row, int direction)
        {
            if (state.RobotRow == row)
            {
                state.SetRobot(row, state.RobotCol + direction);
            }
        }
    }
}
