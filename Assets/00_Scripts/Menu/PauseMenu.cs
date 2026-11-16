namespace ProjectShift.Menu
{
    /// <summary>Rows of the PAUSED panel, top to bottom.</summary>
    public enum PauseItem
    {
        Bgm,
        Sfx,
        Resume,
        Stages
    }

    /// <summary>
    /// Open state and cursor of the PAUSED panel. The cursor starts on BGM at every open and wraps at the ends.
    /// The sound-only panel (title and stage select) has only the BGM and SFX rows.
    /// </summary>
    public class PauseMenu
    {
        private bool isOpen;
        private bool isSoundOnly;
        private PauseItem selected;

        /// <summary>True while the panel shows.</summary>
        public bool IsOpen => isOpen;

        /// <summary>True when the panel was opened from the title or the stage select (BGM and SFX only).</summary>
        public bool IsSoundOnly => isSoundOnly;

        /// <summary>Row under the cursor.</summary>
        public PauseItem Selected => selected;

        /// <summary>Shows the panel with the cursor on BGM; <paramref name="soundOnly"/> leaves out RESUME and STAGES.</summary>
        public void Open(bool soundOnly = false)
        {
            isOpen = true;
            isSoundOnly = soundOnly;
            selected = PauseItem.Bgm;
        }

        /// <summary>Hides the panel.</summary>
        public void Close()
        {
            isOpen = false;
        }

        /// <summary>Moves the cursor <paramref name="delta"/> rows (negative is up), wrapping: up from BGM goes to the last row and back.</summary>
        public void Move(int delta)
        {
            int count = isSoundOnly ? (int)PauseItem.Sfx + 1 : (int)PauseItem.Stages + 1;
            selected = (PauseItem)((((int)selected + delta) % count + count) % count);
        }
    }
}
