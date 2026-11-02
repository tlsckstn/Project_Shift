namespace ProjectShift.Board
{
    /// <summary>
    /// Pure-C# shift rules (no MonoBehaviour, callable from Edit Mode tests).
    /// Changes row values only; robot movement and shift counting live elsewhere.
    /// </summary>
    public static class BoardLogic
    {
        /// <summary>
        /// Shifts a row one cell left (cell 1 moves to cell 0). The right end becomes 0 and the bit pushed past the left end is lost.
        /// </summary>
        /// <example>
        /// BoardLogic.ShiftLeft(state, 0); // 10000000 becomes 00000000
        /// </example>
        public static void ShiftLeft(BoardState state, int row)
        {
            state.SetRow(row, (byte)(state.GetRow(row) << 1));
        }

        /// <summary>
        /// Shifts a row one cell right (cell 0 moves to cell 1). The left end becomes 0 and the bit pushed past the right end is lost.
        /// </summary>
        /// <example>
        /// BoardLogic.ShiftRight(state, 0); // 00000001 becomes 00000000
        /// </example>
        public static void ShiftRight(BoardState state, int row)
        {
            state.SetRow(row, (byte)(state.GetRow(row) >> 1));
        }
    }
}
