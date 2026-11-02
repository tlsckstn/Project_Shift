using System.Collections.Generic;

namespace ProjectShift.Board
{
    /// <summary>
    /// Undo stack and restart point for one stage. Every stored state is a deep copy,
    /// so later moves on the live state never change history.
    /// </summary>
    /// <example>
    /// BoardState before = state.Clone();
    /// if (BoardLogic.TryWalk(state, 1)) history.Save(before); // failed moves leave no empty undo step
    /// state = history.Undo(state);
    /// </example>
    public class BoardHistory
    {
        private readonly BoardState start;
        private readonly Stack<BoardState> undoStack = new Stack<BoardState>();

        /// <summary>Remembers a copy of the stage start state for <see cref="Restart"/>.</summary>
        public BoardHistory(BoardState start)
        {
            this.start = start.Clone();
        }

        /// <summary>Number of undo steps available.</summary>
        public int Count => undoStack.Count;

        /// <summary>Stores a copy of the state from just before a shift, walk or descend.</summary>
        public void Save(BoardState state)
        {
            undoStack.Push(state.Clone());
        }

        /// <summary>Returns the state before the last action, or <paramref name="current"/> unchanged when there is nothing to undo.</summary>
        public BoardState Undo(BoardState current)
        {
            return undoStack.Count > 0 ? undoStack.Pop() : current;
        }

        /// <summary>Clears the undo stack and returns a fresh copy of the stage start state.</summary>
        public BoardState Restart()
        {
            undoStack.Clear();
            return start.Clone();
        }
    }
}
