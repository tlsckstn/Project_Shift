namespace ProjectShift.Board
{
    /// <summary>
    /// Board state: 8-bit rows (cell 0 is the MSB), robot position and remaining shifts.
    /// Not thread-safe.
    /// </summary>
    /// <example>
    /// var state = new BoardState(new byte[] { 0b10000000 }, 0, 0, 3);
    /// bool on = state.GetCell(0, 0); // true
    /// </example>
    public class BoardState
    {
        /// <summary>Number of cells in a row.</summary>
        public const int Columns = 8;

        private readonly byte[] rows;
        private int robotRow;
        private int robotCol;
        private int shiftsLeft;

        /// <summary>Creates a state. The rows array is copied, so the caller keeps its own array.</summary>
        public BoardState(byte[] rows, int robotRow, int robotCol, int shiftsLeft)
        {
            this.rows = (byte[])rows.Clone();
            this.robotRow = robotRow;
            this.robotCol = robotCol;
            this.shiftsLeft = shiftsLeft;
        }

        /// <summary>Number of rows.</summary>
        public int RowCount => rows.Length;

        /// <summary>Row the robot stands on.</summary>
        public int RobotRow => robotRow;

        /// <summary>Column the robot stands on.</summary>
        public int RobotCol => robotCol;

        /// <summary>Shifts the player has left.</summary>
        public int ShiftsLeft => shiftsLeft;

        /// <summary>Returns the raw 8-bit value of row <paramref name="row"/>.</summary>
        public byte GetRow(int row)
        {
            return rows[row];
        }

        /// <summary>Returns true when cell <paramref name="col"/> of row <paramref name="row"/> is on: (rows[r] >> (7 - c)) &amp; 1.</summary>
        public bool GetCell(int row, int col)
        {
            return ((rows[row] >> (Columns - 1 - col)) & 1) == 1;
        }

        /// <summary>Overwrites the raw value of a row. Used by BoardLogic.</summary>
        public void SetRow(int row, byte value)
        {
            rows[row] = value;
        }

        /// <summary>Returns an independent deep copy (the rows array is duplicated).</summary>
        public BoardState Clone()
        {
            return new BoardState(rows, robotRow, robotCol, shiftsLeft);
        }
    }
}
