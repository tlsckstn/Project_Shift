using System.Collections.Generic;
using ProjectShift.Board;
using ProjectShift.Stage;

namespace ProjectShift.Editor
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
        public SolveResult(bool solved, int par, List<SolverAction> actions, int statesVisited)
        {
            Solved = solved;
            Par = par;
            Actions = actions;
            StatesVisited = statesVisited;
        }

        /// <summary>True when the stage can be cleared within its shift limit.</summary>
        public bool Solved { get; }

        /// <summary>Fewest shifts that clear the stage; -1 when not solved.</summary>
        public int Par { get; }

        /// <summary>One shortest solution in play order, empty when not solved.</summary>
        public List<SolverAction> Actions { get; }

        /// <summary>How many distinct states the search expanded (a cost measure).</summary>
        public int StatesVisited { get; }
    }

    /// <summary>
    /// Editor-only breadth-first solver that finds a stage's par (fewest shifts). Walking, descending and switching
    /// robots cost no shift, so it works level by level: all free moves of one shift count first, then each shift.
    /// Every move goes through <see cref="BoardLogic"/>, so the solver never re-implements a rule.
    /// The search is bounded by the stage's shift limit (a stage with limit 0 only solves with no shifts).
    /// </summary>
    /// <example>
    /// SolveResult result = StageSolver.Solve(stageData);
    /// UnityEngine.Debug.Log(result.Par + " via " + string.Join(", ", result.Actions));
    /// </example>
    public static class StageSolver
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

        private readonly struct Entry
        {
            public Entry(BoardState state, long parent, SolverAction action)
            {
                State = state;
                Parent = parent;
                Action = action;
            }

            public BoardState State { get; }

            public long Parent { get; }

            public SolverAction Action { get; }
        }

        /// <summary>Finds the fewest shifts that clear <paramref name="stage"/>, and one solution of that length.</summary>
        public static SolveResult Solve(StageData stage)
        {
            BoardState start = stage.CreateState();
            var visited = new Dictionary<long, Node>();
            var current = new List<Entry> { new Entry(start, -1, default) };
            int shiftsUsed = 0;

            while (current.Count > 0)
            {
                var next = new List<Entry>();
                var queue = new Queue<Entry>(current);
                while (queue.Count > 0)
                {
                    Entry entry = queue.Dequeue();
                    long key = Key(entry.State);
                    // Marked when taken off the queue, so a state first seen via a shift can still be reached free in this level.
                    if (visited.ContainsKey(key))
                    {
                        continue;
                    }

                    visited[key] = new Node(entry.Parent, entry.Action);
                    if (stage.IsCleared(entry.State))
                    {
                        return new SolveResult(true, shiftsUsed, BuildPath(visited, key), visited.Count);
                    }

                    EnqueueFreeMoves(entry.State, key, queue);
                    EnqueueShifts(entry.State, key, next);
                }

                current = next;
                shiftsUsed++;
            }

            return new SolveResult(false, -1, new List<SolverAction>(), visited.Count);
        }

        private static void EnqueueFreeMoves(BoardState state, long key, Queue<Entry> queue)
        {
            BoardState walkLeft = state.Clone();
            if (BoardLogic.TryWalk(walkLeft, -1) && !walkLeft.IsFailed)
            {
                queue.Enqueue(new Entry(walkLeft, key, new SolverAction(SolverActionKind.WalkLeft, 0)));
            }

            BoardState walkRight = state.Clone();
            if (BoardLogic.TryWalk(walkRight, 1) && !walkRight.IsFailed)
            {
                queue.Enqueue(new Entry(walkRight, key, new SolverAction(SolverActionKind.WalkRight, 0)));
            }

            BoardState descended = state.Clone();
            if (BoardLogic.TryDescend(descended) && !descended.IsFailed)
            {
                queue.Enqueue(new Entry(descended, key, new SolverAction(SolverActionKind.Descend, 0)));
            }

            if (state.RobotCount > 1)
            {
                BoardState switched = state.Clone();
                switched.SelectNextRobot();
                queue.Enqueue(new Entry(switched, key, new SolverAction(SolverActionKind.SwitchRobot, 0)));
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
                        next.Add(new Entry(shifted, key, new SolverAction(kind, row)));
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
