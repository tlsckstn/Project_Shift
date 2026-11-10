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
        /// Finds the shift an undo took back: true when one row of <paramref name="undone"/> is that row of
        /// <paramref name="restored"/> shifted one cell. Gives the row and the direction the shift went (1 right, -1 left);
        /// the bit it pushed out is the restored row's cell 7 (right) or cell 0 (left). False for any other undo (walk,
        /// descend), and for a shift of an all-zero row, which changed nothing to show.
        /// </summary>
        public static bool TryFindUndoneShift(BoardState undone, BoardState restored, out int row, out int direction)
        {
            for (row = 0; row < restored.RowCount; row++)
            {
                int now = restored.GetRow(row);
                int was = undone.GetRow(row);
                if (was == now)
                {
                    continue;
                }

                if (was == now >> 1)
                {
                    direction = 1;
                    return true;
                }

                if (was == ((now << 1) & 0xFF))
                {
                    direction = -1;
                    return true;
                }
            }

            direction = 0;
            return false;
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
        /// Succeeds only when the target cell is on, is not a firewall and holds no other robot. Returns false and does nothing otherwise.
        /// </summary>
        public static bool TryWalk(BoardState state, int direction)
        {
            int col = state.RobotCol + direction;
            if (state.IsSelectedRobotFallen || col < 0 || col >= BoardState.Columns || !state.GetCell(state.RobotRow, col)
                || state.IsFirewall(state.RobotRow, col) || IsHeldByOtherRobot(state, state.RobotRow, col))
            {
                return false;
            }

            state.SetRobot(state.RobotRow, col);
            return true;
        }

        /// <summary>
        /// Moves the robot down one row when the cell directly below is on, is not a firewall and holds no other robot.
        /// There is no way up. Returns false and does nothing otherwise.
        /// </summary>
        public static bool TryDescend(BoardState state)
        {
            int row = state.RobotRow + 1;
            if (state.IsSelectedRobotFallen || row >= state.RowCount || !state.GetCell(row, state.RobotCol)
                || state.IsFirewall(row, state.RobotCol) || IsHeldByOtherRobot(state, row, state.RobotCol))
            {
                return false;
            }

            state.SetRobot(row, state.RobotCol);
            return true;
        }

        /// <summary>
        /// Decimal value of a row as the player reads it (cell 0 is the top bit, like the row strings).
        /// With <paramref name="robotsCoverBits"/> every robot standing on the row covers its cell: that cell counts as 0.
        /// A robot always stands on a 1 (it only walks or descends onto on-cells and rides its bit when the row shifts),
        /// so covering lowers the value. It changes the value only, never the terrain. A fallen robot (column outside the row) covers nothing.
        /// </summary>
        public static int GetRowValue(BoardState state, int row, bool robotsCoverBits)
        {
            int value = state.GetRow(row);
            if (!robotsCoverBits)
            {
                return value;
            }

            for (int i = 0; i < state.RobotCount; i++)
            {
                int col = state.GetRobotCol(i);
                if (state.GetRobotRow(i) == row && col >= 0 && col < BoardState.Columns)
                {
                    value &= ~(1 << (BoardState.Columns - 1 - col));
                }
            }

            return value;
        }

        // Robots never share a cell. Only walking and descending can make them meet: a shift moves every robot on a row together.
        private static bool IsHeldByOtherRobot(BoardState state, int row, int col)
        {
            for (int i = 0; i < state.RobotCount; i++)
            {
                if (i != state.SelectedRobot && state.GetRobotRow(i) == row && state.GetRobotCol(i) == col)
                {
                    return true;
                }
            }

            return false;
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
