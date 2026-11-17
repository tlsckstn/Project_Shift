using System.Collections.Generic;
using ProjectShift.Board;

namespace ProjectShift.Stage
{
    /// <summary>One move in a solver solution.</summary>
    public enum SolverActionKind
    {
        WalkLeft,
        WalkRight,
        Descend,
        ShiftLeft,
        ShiftRight,
        SwitchRobot
    }

    /// <summary>One move in a solver solution. <see cref="Row"/> is only used by the two shift kinds.</summary>
    public readonly struct SolverAction
    {
        public SolverAction(SolverActionKind kind, int row)
        {
            Kind = kind;
            Row = row;
        }

        /// <summary>What the player does.</summary>
        public SolverActionKind Kind { get; }

        /// <summary>The row to shift (0 = top); 0 and unused for other kinds.</summary>
        public int Row { get; }

        /// <summary>Short text such as "ShiftLeft r1".</summary>
        public override string ToString()
        {
            return Kind == SolverActionKind.ShiftLeft || Kind == SolverActionKind.ShiftRight ? Kind + " r" + Row : Kind.ToString();
        }
    }

    /// <summary>Outcome of <see cref="StageSolver.Solve"/>.</summary>
    public class SolveResult
    {
        public SolveResult(bool solved, int par, List<SolverAction> actions, int statesVisited, bool tooComplex = false)
        {
            Solved = solved;
            Par = par;
            Actions = actions;
            StatesVisited = statesVisited;
            TooComplex = tooComplex;
        }

        /// <summary>True when the stage can be cleared within its shift limit.</summary>
        public bool Solved { get; }

        /// <summary>True when the search hit its state cap first: neither solved nor proven unsolvable.</summary>
        public bool TooComplex { get; }

        /// <summary>Fewest shifts that clear the stage; -1 when not solved.</summary>
        public int Par { get; }

        /// <summary>One shortest solution in play order, empty when not solved.</summary>
        public List<SolverAction> Actions { get; }

        /// <summary>How many distinct states the search expanded (a cost measure).</summary>
        public int StatesVisited { get; }
    }

    /// <summary>
    /// Breadth-first solver that finds a stage's par (fewest shifts). Walking, descending and switching
    /// robots cost no shift, so it works level by level: all free moves of one shift count first, then each shift.
    /// Every move goes through <see cref="BoardLogic"/>, so the solver never re-implements a rule.
    /// The search is bounded by the stage's shift limit (a stage with limit 0 only solves with no shifts).
    /// WebGL has no threads, so a long search runs a slice per frame with <see cref="Step"/>; <see cref="Solve"/> runs it at once.
    /// </summary>
    /// <example>
    /// SolveResult result = StageSolver.Solve(stageData);
    /// UnityEngine.Debug.Log(result.Par + " via " + string.Join(", ", result.Actions));
    /// // Across frames: var solver = new StageSolver(stageData); each frame: if (solver.Step(2000)) use solver.Result.
    /// </example>
    public class StageSolver
    {
        private readonly struct Node
        {
            public Node(long parent, SolverAction action)
            {
                Parent = parent;
                Action = action;
            }

            public long Parent { get; }

            public SolverAction Action { get; }
        }

        // A queued state as its packed key, not a BoardState: the queues then hold no object references, and the WebGL
        // garbage collector, whose pause grows with the references it must follow, stays fast at 200,000 states
        // (production/qa/evidence/story-004-solver-link/README.md).
        private readonly struct Entry
        {
            public Entry(long key, long parent, SolverAction action)
            {
                Key = key;
                Parent = parent;
                Action = action;
            }

            public long Key { get; }

            public long Parent { get; }

            public SolverAction Action { get; }
        }

        private readonly StageData stage;
        private readonly BoardState start;
        private readonly int stateCap;
        private readonly Dictionary<long, Node> visited = new Dictionary<long, Node>();
        // Queue: the level being searched (states reached with shiftsUsed shifts); next: the states one more shift away.
        private Queue<Entry> queue = new Queue<Entry>();
        private List<Entry> next = new List<Entry>();
        private int shiftsUsed;
        private SolveResult result;

        /// <summary>
        /// Prepares a search of <paramref name="stage"/>; nothing runs until <see cref="Step"/>.
        /// The search gives up with <see cref="SolveResult.TooComplex"/> once <paramref name="stateCap"/> distinct states are visited.
        /// </summary>
        public StageSolver(StageData stage, int stateCap = int.MaxValue)
        {
            this.stage = stage;
            this.stateCap = stateCap;
            start = stage.CreateState();
            queue.Enqueue(new Entry(Key(start), -1, default));
        }

        /// <summary>The outcome once <see cref="Step"/> returned true; null while the search runs.</summary>
        public SolveResult Result => result;

        /// <summary>Distinct states visited so far: the search's progress.</summary>
        public int StatesVisited => visited.Count;

        /// <summary>Finds the fewest shifts that clear <paramref name="stage"/>, and one solution of that length.</summary>
        public static SolveResult Solve(StageData stage)
        {
            var solver = new StageSolver(stage);
            solver.Step(int.MaxValue);
            return solver.Result;
        }

        /// <summary>
        /// Searches up to <paramref name="maxStates"/> more queued states. Returns true once the search is over
        /// (<see cref="Result"/> set); later calls do nothing and return true.
        /// </summary>
        public bool Step(int maxStates)
        {
            for (int i = 0; i < maxStates && result == null; i++)
            {
                if (queue.Count == 0)
                {
                    if (next.Count == 0)
                    {
                        result = new SolveResult(false, -1, new List<SolverAction>(), visited.Count);
                        break;
                    }

                    queue = new Queue<Entry>(next);
                    next = new List<Entry>();
                    shiftsUsed++;
                }

                Entry entry = queue.Dequeue();
                long key = entry.Key;
                // Marked when taken off the queue, so a state first seen via a shift can still be reached free in this level.
                if (visited.ContainsKey(key))
                {
                    continue;
                }

                visited[key] = new Node(entry.Parent, entry.Action);
                BoardState state = Unpack(key);
                if (stage.IsCleared(state))
                {
                    result = new SolveResult(true, shiftsUsed, BuildPath(visited, key), visited.Count);
                    break;
                }

                if (visited.Count >= stateCap)
                {
                    result = new SolveResult(false, -1, new List<SolverAction>(), visited.Count, true);
                    break;
                }

                EnqueueFreeMoves(state, key, queue);
                EnqueueShifts(state, key, next);
            }

            return result != null;
        }

        private static void EnqueueFreeMoves(BoardState state, long key, Queue<Entry> queue)
        {
            BoardState walkLeft = state.Clone();
            if (BoardLogic.TryWalk(walkLeft, -1) && !walkLeft.IsFailed)
            {
                queue.Enqueue(new Entry(Key(walkLeft), key, new SolverAction(SolverActionKind.WalkLeft, 0)));
            }

            BoardState walkRight = state.Clone();
            if (BoardLogic.TryWalk(walkRight, 1) && !walkRight.IsFailed)
            {
                queue.Enqueue(new Entry(Key(walkRight), key, new SolverAction(SolverActionKind.WalkRight, 0)));
            }

            BoardState descended = state.Clone();
            if (BoardLogic.TryDescend(descended) && !descended.IsFailed)
            {
                queue.Enqueue(new Entry(Key(descended), key, new SolverAction(SolverActionKind.Descend, 0)));
            }

            if (state.RobotCount > 1)
            {
                BoardState switched = state.Clone();
                switched.SelectNextRobot();
                queue.Enqueue(new Entry(Key(switched), key, new SolverAction(SolverActionKind.SwitchRobot, 0)));
            }
        }

        private static void EnqueueShifts(BoardState state, long key, List<Entry> next)
        {
            for (int row = 0; row < state.RowCount; row++)
            {
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    BoardState shifted = state.Clone();
                    if (BoardLogic.TryShift(shifted, row, direction) && !shifted.IsFailed)
                    {
                        SolverActionKind kind = direction < 0 ? SolverActionKind.ShiftLeft : SolverActionKind.ShiftRight;
                        next.Add(new Entry(Key(shifted), key, new SolverAction(kind, row)));
                    }
                }
            }
        }

        // Packs rows (8 bits each, up to 3 rows), robots (row 2 bits + column 3 bits each, up to 3 robots) and the
        // selected robot (2 bits) into one long. Failed states are never keyed, so every robot column is 0-7.
        private static long Key(BoardState state)
        {
            long key = 0;
            for (int row = 0; row < state.RowCount; row++)
            {
                key = (key << 8) | state.GetRow(row);
            }

            for (int robot = 0; robot < state.RobotCount; robot++)
            {
                key = (key << 5) | (long)((state.GetRobotRow(robot) << 3) | state.GetRobotCol(robot));
            }

            return (key << 2) | (long)state.SelectedRobot;
        }

        // The reverse of Key for a state of the level being searched: the start state's fixed flags (locked rows,
        // firewalls, bugs) with the key's rows, robots and selection, and the shifts this level has spent.
        private BoardState Unpack(long key)
        {
            BoardState state = start.Clone();
            int selected = (int)(key & 3);
            key >>= 2;
            for (int robot = state.RobotCount - 1; robot >= 0; robot--)
            {
                state.SetRobot(robot, (int)(key >> 3 & 3), (int)(key & 7));
                key >>= 5;
            }

            for (int row = state.RowCount - 1; row >= 0; row--)
            {
                state.SetRow(row, (byte)(key & 0xFF));
                key >>= 8;
            }

            while (state.SelectedRobot != selected)
            {
                state.SelectNextRobot();
            }

            for (int i = 0; i < shiftsUsed; i++)
            {
                state.UseShift();
            }

            return state;
        }

        private static List<SolverAction> BuildPath(Dictionary<long, Node> visited, long goalKey)
        {
            var path = new List<SolverAction>();
            long key = goalKey;
            while (visited[key].Parent != -1)
            {
                path.Add(visited[key].Action);
                key = visited[key].Parent;
            }

            path.Reverse();
            return path;
        }
    }
}
