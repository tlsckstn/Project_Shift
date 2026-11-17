using ProjectShift.Stage;

namespace ProjectShift.Custom
{
    /// <summary>Where a <see cref="StageCheck"/> stands.</summary>
    public enum StageCheckStatus
    {
        Idle,
        Running,
        Solved,
        Unsolvable,
        TooComplex
    }

    /// <summary>
    /// Checks whether a custom stage can be cleared, a slice per frame: WebGL has no threads.
    /// The editor screen calls <see cref="Start"/> on VERIFY and <see cref="Tick"/> once per frame.
    /// </summary>
    public class StageCheck
    {
        /// <summary>
        /// Most distinct states one check visits before giving up as too complex. Measured 2026-11-17:
        /// official stages top out at 22,959 states; the largest worst-case board that finished took 136,692;
        /// boards full of 1s pass 1,000,000 without finishing. See production/qa/evidence/story-004-solver-link/.
        /// </summary>
        public const int StateCap = 200000;

        /// <summary>States per frame. Step(2000) took about 1 ms in WebGL (solver bench 2026-11-17).</summary>
        public const int StatesPerFrame = 2000;

        private StageSolver solver;
        private StageCheckStatus status;
        private int par = -1;
        private int finishedStates;

        /// <summary>Current state of the check.</summary>
        public StageCheckStatus Status => status;

        /// <summary>Fewest shifts that clear the stage once <see cref="Status"/> is Solved; -1 otherwise.</summary>
        public int Par => par;

        /// <summary>Distinct states searched so far (progress), or in total once finished; 0 when idle.</summary>
        public int StatesVisited => solver == null ? finishedStates : solver.StatesVisited;

        /// <summary>
        /// Starts checking <paramref name="stage"/>, dropping any check still running: a result from an earlier board
        /// can never be reported for this one. The caller keeps <paramref name="stage"/> alive until the check ends.
        /// </summary>
        public void Start(StageData stage)
        {
            solver = new StageSolver(stage, StateCap);
            status = StageCheckStatus.Running;
            par = -1;
            finishedStates = 0;
        }

        /// <summary>Drops the running check, if any, and goes back to Idle.</summary>
        public void Cancel()
        {
            solver = null;
            status = StageCheckStatus.Idle;
            par = -1;
            finishedStates = 0;
        }

        /// <summary>Runs one slice of the search; call once per frame. Does nothing unless Running.</summary>
        public void Tick()
        {
            if (status != StageCheckStatus.Running || !solver.Step(StatesPerFrame))
            {
                return;
            }

            SolveResult result = solver.Result;
            status = result.Solved ? StageCheckStatus.Solved : result.TooComplex ? StageCheckStatus.TooComplex : StageCheckStatus.Unsolvable;
            par = result.Par;
            finishedStates = result.StatesVisited;
            // The search holds every visited state (about 50 MB at the cap in WebGL); let it go now.
            solver = null;
        }
    }
}
