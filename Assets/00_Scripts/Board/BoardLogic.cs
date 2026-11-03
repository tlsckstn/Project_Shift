namespace ProjectShift.Board
{
    /// <summary>
    /// Pure-C# board rules (no MonoBehaviour, callable from Edit Mode tests):
    /// row shifts, walking and descending (selected robot only), and falling off a row end.
    /// </summary>
    public static class BoardLogic
    {
        /// <summary>
        /// Shifts a row (<paramref name="direction"/> is -1 for left, 1 for right) and spends one shift.
        /// A shift that only pushes out lost bits still costs one. Returns false and does nothing when no shifts are left
        /// or the row is locked (a locked row costs nothing).
        /// </summary>
        public static bool TryShift(BoardState state, int row, int direction)
        {
            if (state.ShiftsLeft <= 0 || state.IsRowLocked(row))
            {
                return false;
            }

            if (direction < 0)
            {
                ShiftLeft(state, row);
            }
            else
            {
                ShiftRight(state, row);
            }

            state.UseShift();
            return true;
        }

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
        /// Succeeds only when the target cell is on and is not a firewall. Returns false and does nothing otherwise.
        /// </summary>
        public static bool TryWalk(BoardState state, int direction)
        {
            int col = state.RobotCol + direction;
            if (state.IsSelectedRobotFallen || col < 0 || col >= BoardState.Columns || !state.GetCell(state.RobotRow, col)
                || state.IsFirewall(state.RobotRow, col))
            {
                return false;
            }

            state.SetRobot(state.RobotRow, col);
            return true;
        }

        /// <summary>
        /// Moves the robot down one row when the cell directly below is on and is not a firewall. There is no way up.
        /// Returns false and does nothing otherwise.
        /// </summary>
        public static bool TryDescend(BoardState state)
        {
            int row = state.RobotRow + 1;
            if (state.IsSelectedRobotFallen || row >= state.RowCount || !state.GetCell(row, state.RobotCol)
                || state.IsFirewall(row, state.RobotCol))
            {
                return false;
            }

            state.SetRobot(row, state.RobotCol);
            return true;
        }

        // Every robot on the row rides it, even when the cell under it was shifted away; it only falls past an end.
        // Firewalls never stop a ride: a robot may be carried into or through one.
        private static void MoveRobotWithRow(BoardState state, int row, int direction)
        {
            for (int i = 0; i < state.RobotCount; i++)
            {
                if (state.GetRobotRow(i) == row)
                {
                    state.SetRobot(i, row, state.GetRobotCol(i) + direction);
                }
            }
        }
    }
}
