using System;
using ProjectShift.Board;

namespace ProjectShift.Custom
{
    /// <summary>
    /// What a stage code carries: the board, robots, limit and gimmicks. No hint, chapter title or par.
    /// A cell is one int, row * <see cref="BoardState.Columns"/> + column; row 0 is the top row, column 0 the leftmost cell.
    /// </summary>
    public sealed class StageCodeData
    {
        private readonly byte[] rows;
        private readonly int[] starts;
        private readonly int[] exits;
        private readonly int shiftLimit;
        private readonly int[] lockedRows;
        private readonly int[] firewalls;
        private readonly int[] bugCells;
        private readonly int[] lockRows;
        private readonly int[] lockTargets;
        private readonly bool robotsCoverBits;

        /// <summary>Null arrays become empty ones. Nothing is checked here; <see cref="StageCode.Validate"/> does that.</summary>
        public StageCodeData(byte[] rows, int[] starts, int[] exits, int shiftLimit, int[] lockedRows, int[] firewalls,
            int[] bugCells, int[] lockRows, int[] lockTargets, bool robotsCoverBits)
        {
            this.rows = rows ?? Array.Empty<byte>();
            this.starts = starts ?? Array.Empty<int>();
            this.exits = exits ?? Array.Empty<int>();
            this.shiftLimit = shiftLimit;
            this.lockedRows = lockedRows ?? Array.Empty<int>();
            this.firewalls = firewalls ?? Array.Empty<int>();
            this.bugCells = bugCells ?? Array.Empty<int>();
            this.lockRows = lockRows ?? Array.Empty<int>();
            this.lockTargets = lockTargets ?? Array.Empty<int>();
            this.robotsCoverBits = robotsCoverBits;
        }

        /// <summary>One byte per row, top row first; the high bit is the leftmost cell.</summary>
        public byte[] Rows => rows;

        /// <summary>Start cell of each robot, in robot order.</summary>
        public int[] Starts => starts;

        /// <summary>Exit cell of each robot, in robot order.</summary>
        public int[] Exits => exits;

        /// <summary>Shifts the stage starts with.</summary>
        public int ShiftLimit => shiftLimit;

        /// <summary>Row indices that cannot be shifted, ascending after a parse.</summary>
        public int[] LockedRows => lockedRows;

        /// <summary>Firewall cells.</summary>
        public int[] Firewalls => firewalls;

        /// <summary>Bug cells.</summary>
        public int[] BugCells => bugCells;

        /// <summary>Row of each number lock; <see cref="LockTargets"/> holds its target at the same index.</summary>
        public int[] LockRows => lockRows;

        /// <summary>Target decimal value (0-255) of each number lock.</summary>
        public int[] LockTargets => lockTargets;

        /// <summary>True when a robot's cell counts as 0 in row values.</summary>
        public bool RobotsCoverBits => robotsCoverBits;
    }

    /// <summary>
    /// Stage code: <see cref="StageCodeData"/> packed into bytes and written as base64url (A-Z a-z 0-9 - _),
    /// so it survives copy and paste. Layout, one byte each unless noted:
    /// version (1); header (bits 0-1 rows, 2-3 robots, 4-5 exits, 6 robots cover bits, 7 zero);
    /// rows; robot starts; robot exits; shift limit; locked rows bitmask (bit r = row r);
    /// firewall count + cells; bug count + cells; number lock count + (row, target) pairs.
    /// </summary>
    public static class StageCode
    {
        /// <summary>Most robots on one board.</summary>
        public const int MaxRobots = 3;

        /// <summary>
        /// Highest shift limit a code may carry. Official stages top out at 13; 99 leaves editor room
        /// and still fits a two-digit counter.
        /// </summary>
        public const int MaxShiftLimit = 99;

        /// <summary>Longest code read, after trimming. A full 3-row board with every list full is about 100 characters.</summary>
        public const int MaxCodeLength = 160;

        private const byte Version = 1;
        private const int CellCount = BoardView.MaxRows * BoardState.Columns;

        /// <summary>Writes <paramref name="data"/> as a code. Expects data that passes <see cref="Validate"/>.</summary>
        public static string Encode(StageCodeData data)
        {
            int size = 2 + data.Rows.Length + data.Starts.Length + data.Exits.Length + 2
                + 1 + data.Firewalls.Length + 1 + data.BugCells.Length + 1 + data.LockRows.Length * 2;
            byte[] bytes = new byte[size];
            int at = 0;
            bytes[at++] = Version;
            bytes[at++] = (byte)(data.Rows.Length | data.Starts.Length << 2 | data.Exits.Length << 4 | (data.RobotsCoverBits ? 1 << 6 : 0));
            foreach (byte row in data.Rows)
            {
                bytes[at++] = row;
            }

            at = WriteCells(bytes, at, data.Starts);
            at = WriteCells(bytes, at, data.Exits);
            bytes[at++] = (byte)data.ShiftLimit;
            int lockedMask = 0;
            foreach (int row in data.LockedRows)
            {
                lockedMask |= 1 << row;
            }

            bytes[at++] = (byte)lockedMask;
            bytes[at++] = (byte)data.Firewalls.Length;
            at = WriteCells(bytes, at, data.Firewalls);
            bytes[at++] = (byte)data.BugCells.Length;
            at = WriteCells(bytes, at, data.BugCells);
            bytes[at++] = (byte)data.LockRows.Length;
            for (int i = 0; i < data.LockRows.Length; i++)
            {
                bytes[at++] = (byte)data.LockRows[i];
                bytes[at++] = (byte)data.LockTargets[i];
            }

            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Reads a code. Leading and trailing whitespace is ignored. Never throws: on any bad input it returns false,
        /// <paramref name="data"/> null and <paramref name="error"/> the reason.
        /// </summary>
        public static bool TryParse(string code, out StageCodeData data, out string error)
        {
            data = null;
            error = Decode(code?.Trim(), out byte[] bytes);
            if (error == null)
            {
                error = Read(bytes, out data);
            }

            if (error == null)
            {
                error = Validate(data);
            }

            if (error != null)
            {
                data = null;
            }

            return error == null;
        }

        /// <summary>Checks the stage rules a code must keep. Returns null when the data is valid, otherwise the reason.</summary>
        public static string Validate(StageCodeData data)
        {
            int rowCount = data.Rows.Length;
            if (rowCount < 1 || rowCount > BoardView.MaxRows)
            {
                return $"Needs 1-{BoardView.MaxRows} rows, has {rowCount}.";
            }

            int robots = data.Starts.Length;
            if (robots < 1 || robots > MaxRobots)
            {
                return $"Needs 1-{MaxRobots} robots, has {robots}.";
            }

            if (data.Exits.Length != robots)
            {
                return $"{robots} robots but {data.Exits.Length} exits.";
            }

            if (data.ShiftLimit < 0 || data.ShiftLimit > MaxShiftLimit)
            {
                return $"Shift limit {data.ShiftLimit} is outside 0-{MaxShiftLimit}.";
            }

            int cells = rowCount * BoardState.Columns;
            string offBoard = FindOffBoard("Robot start", data.Starts, cells) ?? FindOffBoard("Exit", data.Exits, cells)
                ?? FindOffBoard("Firewall", data.Firewalls, cells) ?? FindOffBoard("Bug cell", data.BugCells, cells);
            if (offBoard != null)
            {
                return offBoard;
            }

            for (int i = 0; i < robots; i++)
            {
                int start = data.Starts[i];
                if (((data.Rows[start / BoardState.Columns] >> (BoardState.Columns - 1 - start % BoardState.Columns)) & 1) == 0)
                {
                    return $"Robot {i} starts on a 0 cell.";
                }

                for (int j = 0; j < i; j++)
                {
                    if (data.Starts[j] == start)
                    {
                        return $"Robots {j} and {i} start on the same cell.";
                    }
                }
            }

            foreach (int row in data.LockedRows)
            {
                if (row < 0 || row >= rowCount)
                {
                    return $"Locked row {row} is off the board.";
                }
            }

            if (data.LockTargets.Length != data.LockRows.Length)
            {
                return $"{data.LockRows.Length} number lock rows but {data.LockTargets.Length} targets.";
            }

            for (int i = 0; i < data.LockRows.Length; i++)
            {
                int row = data.LockRows[i];
                if (row < 0 || row >= rowCount)
                {
                    return $"Number lock row {row} is off the board.";
                }

                if (data.LockTargets[i] < 0 || data.LockTargets[i] > 255)
                {
                    return $"Number lock target {data.LockTargets[i]} is outside 0-255.";
                }

                for (int j = 0; j < i; j++)
                {
                    if (data.LockRows[j] == row)
                    {
                        return $"Row {row} has two number locks.";
                    }
                }
            }

            return null;
        }

        private static int WriteCells(byte[] bytes, int at, int[] cells)
        {
            foreach (int cell in cells)
            {
                bytes[at++] = (byte)cell;
            }

            return at;
        }

        private static string FindOffBoard(string what, int[] cellList, int cells)
        {
            foreach (int cell in cellList)
            {
                if (cell < 0 || cell >= cells)
                {
                    return $"{what} cell {cell} is off the {cells / BoardState.Columns}-row board.";
                }
            }

            return null;
        }

        // Text to bytes. Only base64url characters pass, so no input can reach Convert with a character it rejects.
        private static string Decode(string code, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(code))
            {
                return "Code is empty.";
            }

            if (code.Length > MaxCodeLength)
            {
                return $"Code is {code.Length} characters, longer than {MaxCodeLength}.";
            }

            foreach (char c in code)
            {
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok)
                {
                    return $"Code has a character that is not allowed: '{c}'.";
                }
            }

            string base64 = code.Replace('-', '+').Replace('_', '/').PadRight(code.Length + (4 - code.Length % 4) % 4, '=');
            byte[] buffer = new byte[base64.Length / 4 * 3];
            if (!Convert.TryFromBase64String(base64, buffer, out int written))
            {
                return "Code has the wrong length.";
            }

            bytes = new byte[written];
            Array.Copy(buffer, bytes, written);
            return null;
        }

        // Bytes to data. Every read is bounds-checked, so a cut-off code fails instead of throwing.
        private static string Read(byte[] bytes, out StageCodeData data)
        {
            data = null;
            int at = 0;
            if (bytes.Length < 2)
            {
                return "Code is too short.";
            }

            if (bytes[at++] != Version)
            {
                return $"Unknown code version {bytes[0]}.";
            }

            int header = bytes[at++];
            if ((header & 0x80) != 0)
            {
                return "Code header is not valid.";
            }

            int rowCount = header & 3;
            int robots = header >> 2 & 3;
            int exitCount = header >> 4 & 3;
            bool cover = (header & 1 << 6) != 0;

            byte[] rows = new byte[rowCount];
            int[] starts = new int[robots];
            int[] exits = new int[exitCount];
            if (!TryRead(bytes, ref at, rows) || !TryRead(bytes, ref at, starts) || !TryRead(bytes, ref at, exits)
                || at + 2 > bytes.Length)
            {
                return "Code is too short.";
            }

            int shiftLimit = bytes[at++];
            int lockedMask = bytes[at++];
            int[] lockedRows = new int[CountBits(lockedMask)];
            for (int row = 0, i = 0; row < 8; row++)
            {
                if ((lockedMask & 1 << row) != 0)
                {
                    lockedRows[i++] = row;
                }
            }

            if (!TryReadList(bytes, ref at, out int[] firewalls) || !TryReadList(bytes, ref at, out int[] bugCells)
                || at >= bytes.Length)
            {
                return "Code is cut off or a cell list is too long.";
            }

            int lockCount = bytes[at++];
            if (lockCount > BoardView.MaxRows)
            {
                return $"Code has {lockCount} number locks, more than {BoardView.MaxRows}.";
            }

            int[] lockRows = new int[lockCount];
            int[] lockTargets = new int[lockCount];
            for (int i = 0; i < lockCount; i++)
            {
                if (at + 2 > bytes.Length)
                {
                    return "Code is too short.";
                }

                lockRows[i] = bytes[at++];
                lockTargets[i] = bytes[at++];
            }

            if (at != bytes.Length)
            {
                return "Code has extra data at the end.";
            }

            data = new StageCodeData(rows, starts, exits, shiftLimit, lockedRows, firewalls, bugCells, lockRows, lockTargets, cover);
            return null;
        }

        private static bool TryRead(byte[] bytes, ref int at, byte[] into)
        {
            if (at + into.Length > bytes.Length)
            {
                return false;
            }

            Array.Copy(bytes, at, into, 0, into.Length);
            at += into.Length;
            return true;
        }

        private static bool TryRead(byte[] bytes, ref int at, int[] into)
        {
            if (at + into.Length > bytes.Length)
            {
                return false;
            }

            for (int i = 0; i < into.Length; i++)
            {
                into[i] = bytes[at++];
            }

            return true;
        }

        // A count byte, then that many cells. The count is capped at the largest board so a bad byte cannot ask for 255 cells.
        private static bool TryReadList(byte[] bytes, ref int at, out int[] cells)
        {
            cells = null;
            if (at >= bytes.Length || bytes[at] > CellCount)
            {
                return false;
            }

            cells = new int[bytes[at++]];
            return TryRead(bytes, ref at, cells);
        }

        private static int CountBits(int value)
        {
            int count = 0;
            for (; value != 0; value &= value - 1)
            {
                count++;
            }

            return count;
        }
    }
}
