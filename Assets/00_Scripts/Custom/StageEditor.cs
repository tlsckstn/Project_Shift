using System.Collections.Generic;
using ProjectShift.Board;

namespace ProjectShift.Custom
{
    /// <summary>
    /// The board being edited in the stage editor, and every edit the keys can make. Plain C#, so the rules are testable
    /// without the screen. Cells are row * <see cref="BoardState.Columns"/> + column, as in <see cref="StageCodeData"/>.
    /// Robots are numbered from 0 here and shown from 1.
    /// </summary>
    public class StageEditor
    {
        private const int Columns = BoardState.Columns;
        private const int MaxRows = BoardView.MaxRows;
        private const int CellCount = MaxRows * Columns;

        private readonly byte[] rows = new byte[MaxRows];
        // Start cell of each placed robot; robots are always 0..Count-1 with no gaps.
        private readonly List<int> robots = new List<int>();
        // Exit cell per robot, -1 when not placed yet.
        private readonly int[] exits = { -1, -1, -1 };
        private readonly bool[] bugs = new bool[CellCount];
        private readonly bool[] firewalls = new bool[CellCount];
        private readonly bool[] lockedRows = new bool[MaxRows];
        // Number-lock target per row, -1 for none.
        private readonly int[] lockTargets = { -1, -1, -1 };
        private int rowCount = 1;
        private int shiftLimit = 5;
        private bool robotsCoverBits;
        private int cursorRow;
        private int cursorCol;

        /// <summary>Rows on the board, 1 to <see cref="BoardView.MaxRows"/>.</summary>
        public int RowCount => rowCount;

        /// <summary>Row of the cell cursor.</summary>
        public int CursorRow => cursorRow;

        /// <summary>Column of the cell cursor.</summary>
        public int CursorCol => cursorCol;

        /// <summary>Shifts the stage starts with.</summary>
        public int ShiftLimit => shiftLimit;

        /// <summary>True when robots cover the cell they stand on (it counts as 0).</summary>
        public bool RobotsCoverBits => robotsCoverBits;

        /// <summary>Robots placed so far.</summary>
        public int RobotCount => robots.Count;

        /// <summary>Exit cell of robot <paramref name="robot"/>, -1 when it has none yet.</summary>
        public int GetExit(int robot)
        {
            return exits[robot];
        }

        /// <summary>Number-lock target of <paramref name="row"/>, -1 when the row has none.</summary>
        public int GetLockTarget(int row)
        {
            return lockTargets[row];
        }

        /// <summary>Moves the cursor by whole cells, stopping at the board edge.</summary>
        public void MoveCursor(int rowDelta, int colDelta)
        {
            cursorRow = Clamp(cursorRow + rowDelta, 0, rowCount - 1);
            cursorCol = Clamp(cursorCol + colDelta, 0, Columns - 1);
        }

        /// <summary>Turns the bit under the cursor on or off.</summary>
        public void ToggleBit()
        {
            rows[cursorRow] ^= (byte)(1 << (Columns - 1 - cursorCol));
        }

        /// <summary>Adds or removes rows at the bottom. A removed row takes everything on it with it.</summary>
        public void ChangeRowCount(int delta)
        {
            int newCount = Clamp(rowCount + delta, 1, MaxRows);
            for (int row = newCount; row < rowCount; row++)
            {
                rows[row] = 0;
                lockedRows[row] = false;
                lockTargets[row] = -1;
                for (int col = 0; col < Columns; col++)
                {
                    bugs[row * Columns + col] = false;
                    firewalls[row * Columns + col] = false;
                }
            }

            int firstGone = newCount * Columns;
            for (int robot = robots.Count - 1; robot >= 0; robot--)
            {
                if (robots[robot] >= firstGone)
                {
                    RemoveRobot(robot);
                }
                else if (exits[robot] >= firstGone)
                {
                    exits[robot] = -1;
                }
            }

            rowCount = newCount;
            cursorRow = Clamp(cursorRow, 0, rowCount - 1);
        }

        /// <summary>
        /// Key 1-3 (<paramref name="robot"/> 0-2): places the next robot, moves an existing one to the cursor, or removes it when
        /// it already stands there (later robots then move down a number). A robot past the next free number is ignored.
        /// </summary>
        public void ToggleRobot(int robot)
        {
            int cell = CursorCell;
            if (robot < robots.Count)
            {
                if (robots[robot] == cell)
                {
                    RemoveRobot(robot);
                }
                else
                {
                    robots[robot] = cell;
                }
            }
            else if (robot == robots.Count)
            {
                robots.Add(cell);
            }
        }

        /// <summary>Shift + key 1-3: puts robot <paramref name="robot"/>'s exit on the cursor, or removes it when it is already there.</summary>
        public void ToggleExit(int robot)
        {
            if (robot < robots.Count)
            {
                exits[robot] = exits[robot] == CursorCell ? -1 : CursorCell;
            }
        }

        /// <summary>Adds or removes a bug cell under the cursor.</summary>
        public void ToggleBug()
        {
            bugs[CursorCell] = !bugs[CursorCell];
        }

        /// <summary>Adds or removes a firewall under the cursor.</summary>
        public void ToggleFirewall()
        {
            firewalls[CursorCell] = !firewalls[CursorCell];
        }

        /// <summary>Locks or unlocks the cursor row against shifting.</summary>
        public void ToggleLockedRow()
        {
            lockedRows[cursorRow] = !lockedRows[cursorRow];
        }

        /// <summary>
        /// Gives the cursor row a number lock whose goal is the row's value right now, or removes the lock it has.
        /// Build the goal pattern, press N, then edit the row into its starting pattern.
        /// </summary>
        public void ToggleNumberLock()
        {
            lockTargets[cursorRow] = lockTargets[cursorRow] >= 0 ? -1 : rows[cursorRow];
        }

        /// <summary>Raises or lowers the shift limit within 0 to <see cref="StageCode.MaxShiftLimit"/>.</summary>
        public void ChangeShiftLimit(int delta)
        {
            shiftLimit = Clamp(shiftLimit + delta, 0, StageCode.MaxShiftLimit);
        }

        /// <summary>Turns the robot cover rule on or off.</summary>
        public void ToggleCover()
        {
            robotsCoverBits = !robotsCoverBits;
        }

        /// <summary>
        /// Why the board cannot be checked or played yet, as one short line for the screen; null when it is a valid stage.
        /// </summary>
        public string Reason
        {
            get
            {
                if (robots.Count == 0)
                {
                    return "PLACE A ROBOT";
                }

                for (int i = 0; i < robots.Count; i++)
                {
                    int cell = robots[i];
                    if (((rows[cell / Columns] >> (Columns - 1 - cell % Columns)) & 1) == 0)
                    {
                        return $"ROBOT {i + 1} IS ON A 0 CELL";
                    }

                    // StageCode accepts this, but the robot would fail before the first move.
                    if (bugs[cell])
                    {
                        return $"ROBOT {i + 1} IS ON A BUG";
                    }

                    for (int j = 0; j < i; j++)
                    {
                        if (robots[j] == cell)
                        {
                            return $"ROBOTS {j + 1} AND {i + 1} SHARE A CELL";
                        }
                    }

                    if (exits[i] < 0)
                    {
                        return $"ROBOT {i + 1} NEEDS AN EXIT";
                    }
                }

                // The checks above cover every rule the editor can break; this is the safety net for the code format's own.
                string error = StageCode.Validate(ToCodeData());
                return error?.ToUpperInvariant();
            }
        }

        /// <summary>The board as stage code data. Only meaningful once <see cref="Reason"/> is null.</summary>
        public StageCodeData ToCodeData()
        {
            byte[] codeRows = new byte[rowCount];
            System.Array.Copy(rows, codeRows, rowCount);
            int[] codeExits = new int[robots.Count];
            System.Array.Copy(exits, codeExits, robots.Count);
            var locked = new List<int>();
            var lockRows = new List<int>();
            var targets = new List<int>();
            for (int row = 0; row < rowCount; row++)
            {
                if (lockedRows[row])
                {
                    locked.Add(row);
                }

                if (lockTargets[row] >= 0)
                {
                    lockRows.Add(row);
                    targets.Add(lockTargets[row]);
                }
            }

            return new StageCodeData(codeRows, robots.ToArray(), codeExits, shiftLimit, locked.ToArray(), Cells(firewalls), Cells(bugs),
                lockRows.ToArray(), targets.ToArray(), robotsCoverBits);
        }

        /// <summary>Replaces the whole board with <paramref name="data"/>, which must pass <see cref="StageCode.Validate"/>. The cursor goes to the top left.</summary>
        public void Load(StageCodeData data)
        {
            rowCount = data.Rows.Length;
            System.Array.Clear(rows, 0, MaxRows);
            System.Array.Copy(data.Rows, rows, rowCount);
            robots.Clear();
            robots.AddRange(data.Starts);
            for (int i = 0; i < exits.Length; i++)
            {
                exits[i] = i < data.Exits.Length ? data.Exits[i] : -1;
            }

            System.Array.Clear(bugs, 0, CellCount);
            foreach (int cell in data.BugCells)
            {
                bugs[cell] = true;
            }

            System.Array.Clear(firewalls, 0, CellCount);
            foreach (int cell in data.Firewalls)
            {
                firewalls[cell] = true;
            }

            System.Array.Clear(lockedRows, 0, MaxRows);
            foreach (int row in data.LockedRows)
            {
                lockedRows[row] = true;
            }

            for (int i = 0; i < lockTargets.Length; i++)
            {
                lockTargets[i] = -1;
            }

            for (int i = 0; i < data.LockRows.Length; i++)
            {
                lockTargets[data.LockRows[i]] = data.LockTargets[i];
            }

            shiftLimit = data.ShiftLimit;
            robotsCoverBits = data.RobotsCoverBits;
            cursorRow = 0;
            cursorCol = 0;
        }

        /// <summary>The board as a playable state for drawing (robots may stand anywhere while editing).</summary>
        public BoardState CreateState()
        {
            byte[] stateRows = new byte[rowCount];
            System.Array.Copy(rows, stateRows, rowCount);
            int[] robotRows = new int[robots.Count];
            int[] robotCols = new int[robots.Count];
            for (int i = 0; i < robots.Count; i++)
            {
                robotRows[i] = robots[i] / Columns;
                robotCols[i] = robots[i] % Columns;
            }

            bool[] locked = new bool[rowCount];
            System.Array.Copy(lockedRows, locked, rowCount);
            bool[] stateFirewalls = new bool[rowCount * Columns];
            System.Array.Copy(firewalls, stateFirewalls, stateFirewalls.Length);
            bool[] stateBugs = new bool[rowCount * Columns];
            System.Array.Copy(bugs, stateBugs, stateBugs.Length);
            return new BoardState(stateRows, robotRows, robotCols, shiftLimit, locked, stateFirewalls, stateBugs);
        }

        private int CursorCell => cursorRow * Columns + cursorCol;

        private void RemoveRobot(int robot)
        {
            robots.RemoveAt(robot);
            for (int i = robot; i < exits.Length - 1; i++)
            {
                exits[i] = exits[i + 1];
            }

            exits[exits.Length - 1] = -1;
        }

        private int[] Cells(bool[] flags)
        {
            var cells = new List<int>();
            for (int cell = 0; cell < rowCount * Columns; cell++)
            {
                if (flags[cell])
                {
                    cells.Add(cell);
                }
            }

            return cells.ToArray();
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
