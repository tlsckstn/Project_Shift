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

    /// <summary>Open state and cursor of the PAUSED panel. The cursor starts on BGM at every open and stops at the ends.</summary>
    public class PauseMenu
    {
        private bool isOpen;
        private PauseItem selected;

        /// <summary>True while the panel shows.</summary>
        public bool IsOpen => isOpen;

        /// <summary>Row under the cursor.</summary>
        public PauseItem Selected => selected;

        /// <summary>Shows the panel with the cursor on BGM.</summary>
        public void Open()
        {
            isOpen = true;
            selected = PauseItem.Bgm;
        }

        /// <summary>Hides the panel.</summary>
        public void Close()
        {
            isOpen = false;
        }

        /// <summary>Moves the cursor <paramref name="delta"/> rows (negative is up), without wrapping.</summary>
        public void Move(int delta)
        {
            int row = (int)selected + delta;
            if (row < (int)PauseItem.Bgm)
            {
                row = (int)PauseItem.Bgm;
            }
            else if (row > (int)PauseItem.Stages)
            {
                row = (int)PauseItem.Stages;
            }

            selected = (PauseItem)row;
        }
    }
}
