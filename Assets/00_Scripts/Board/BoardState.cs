namespace ProjectShift.Board
{
    /// <summary>
    /// Board state: 8-bit rows (cell 0 is the MSB), robot positions (one or more, one of them selected) and remaining shifts.
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
        // One entry per robot. Walking and descending move only the selected one; shifts move every robot on the row.
        private readonly int[] robotRows;
        private readonly int[] robotCols;
        private int selectedRobot;
        private int shiftsLeft;
        // Fixed for the whole stage, so clones share it. Null means no locked rows.
        private readonly bool[] lockedRows;
        // Indexed row * Columns + column. Fixed for the whole stage, so clones share it. Null means no firewalls.
        private readonly bool[] firewalls;
        // Same layout and lifetime as firewalls. Null means no bug cells.
        private readonly bool[] bugCells;

        /// <summary>
        /// Creates a state. The rows array is copied, so the caller keeps its own array.
        /// <paramref name="lockedRows"/> (one flag per row), <paramref name="firewalls"/> and <paramref name="bugCells"/>
        /// (one flag per cell, row * Columns + column) may be null; they are kept by reference and must not change afterwards.
        /// </summary>
        public BoardState(byte[] rows, int robotRow, int robotCol, int shiftsLeft, bool[] lockedRows = null, bool[] firewalls = null, bool[] bugCells = null)
            : this(rows, new[] { robotRow }, new[] { robotCol }, shiftsLeft, lockedRows, firewalls, bugCells)
        {
        }

        /// <summary>
        /// Creates a state with several robots: robot i stands at (<paramref name="robotRows"/>[i], <paramref name="robotCols"/>[i]).
        /// The robot arrays are copied. <paramref name="selectedRobot"/> is the robot the player controls.
        /// </summary>
        public BoardState(byte[] rows, int[] robotRows, int[] robotCols, int shiftsLeft, bool[] lockedRows = null, bool[] firewalls = null, bool[] bugCells = null, int selectedRobot = 0)
        {
            this.rows = (byte[])rows.Clone();
            this.lockedRows = lockedRows;
            this.firewalls = firewalls;
            this.bugCells = bugCells;
            this.robotRows = (int[])robotRows.Clone();
            this.robotCols = (int[])robotCols.Clone();
            this.selectedRobot = selectedRobot;
            this.shiftsLeft = shiftsLeft;
        }

        /// <summary>Number of rows.</summary>
        public int RowCount => rows.Length;

        /// <summary>Number of robots on the board.</summary>
        public int RobotCount => robotRows.Length;

        /// <summary>Index of the robot the player controls.</summary>
        public int SelectedRobot => selectedRobot;

        /// <summary>Row the selected robot stands on.</summary>
        public int RobotRow => robotRows[selectedRobot];

        /// <summary>Column the selected robot stands on.</summary>
        public int RobotCol => robotCols[selectedRobot];

        /// <summary>Row robot <paramref name="robot"/> stands on.</summary>
        public int GetRobotRow(int robot)
        {
            return robotRows[robot];
        }

        /// <summary>Column robot <paramref name="robot"/> stands on.</summary>
        public int GetRobotCol(int robot)
        {
            return robotCols[robot];
        }

        /// <summary>Shifts the player has left. One count is shared by all robots.</summary>
        public int ShiftsLeft => shiftsLeft;

        /// <summary>True when the selected robot was pushed past cell 0 or cell 7. The column stays out of range until restart.</summary>
        public bool IsSelectedRobotFallen => IsFallen(selectedRobot);

        /// <summary>True when any robot was pushed past cell 0 or cell 7. The column stays out of range until restart.</summary>
        public bool IsRobotFallen
        {
            get
            {
                for (int i = 0; i < robotCols.Length; i++)
                {
                    if (IsFallen(i))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True when any robot entered a bug cell, by any move.</summary>
        public bool IsRobotOnBug
        {
            get
            {
                for (int i = 0; i < robotCols.Length; i++)
                {
                    if (!IsFallen(i) && IsBugCell(robotRows[i], robotCols[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True when the stage is lost and must restart: a robot fell or entered a bug cell.</summary>
        public bool IsFailed => IsRobotFallen || IsRobotOnBug;

        private bool IsFallen(int robot)
        {
            return robotCols[robot] < 0 || robotCols[robot] >= Columns;
        }

        /// <summary>True when row <paramref name="row"/> cannot be shifted.</summary>
        public bool IsRowLocked(int row)
        {
            return lockedRows != null && lockedRows[row];
        }

        /// <summary>True when board cell (<paramref name="row"/>, <paramref name="col"/>) is a firewall. Firewalls are fixed to the board and do not move with shifts.</summary>
        public bool IsFirewall(int row, int col)
        {
            return firewalls != null && firewalls[row * Columns + col];
        }

        /// <summary>True when board cell (<paramref name="row"/>, <paramref name="col"/>) is a bug cell. Bug cells are fixed to the board and do not move with shifts.</summary>
        public bool IsBugCell(int row, int col)
        {
            return bugCells != null && bugCells[row * Columns + col];
        }

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

        /// <summary>Moves the selected robot. Used by BoardLogic; no bounds check, so a fall can be represented.</summary>
        public void SetRobot(int row, int col)
        {
            SetRobot(selectedRobot, row, col);
        }

        /// <summary>Moves robot <paramref name="robot"/>. Used by BoardLogic; no bounds check, so a fall can be represented.</summary>
        public void SetRobot(int robot, int row, int col)
        {
            robotRows[robot] = row;
            robotCols[robot] = col;
        }

        /// <summary>Selects the next robot, wrapping around. Costs no shift; with one robot it changes nothing.</summary>
        public void SelectNextRobot()
        {
            selectedRobot = (selectedRobot + 1) % robotRows.Length;
        }

        /// <summary>Spends one shift. Used by BoardLogic, which checks the count first.</summary>
        public void UseShift()
        {
            shiftsLeft--;
        }

        /// <summary>Returns an independent copy (the rows and robot arrays are duplicated, the selection is kept; the fixed locked-row, firewall and bug-cell flags are shared).</summary>
        public BoardState Clone()
        {
            return new BoardState(rows, robotRows, robotCols, shiftsLeft, lockedRows, firewalls, bugCells, selectedRobot);
        }
    }
}
