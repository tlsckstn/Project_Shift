using ProjectShift.Audio;
using ProjectShift.Menu;
using ProjectShift.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectShift.Input
{
    /// <summary>
    /// Maps the project-wide "Puzzle" action map to stage actions: walk, descend, select a row, shift it, switch robot, undo, restart, next stage.
    /// Bindings live in Assets/InputSystem_Actions.inputactions.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] private StageManager stageManager;
        [SerializeField] private SoundPlayer sound;
        [SerializeField] private PauseMenuView pauseView;
        [Tooltip("Seconds Left / Right must be held in the pause menu before the volume starts repeating.")]
        [SerializeField] private float repeatDelay = 0.3f;
        [Tooltip("Seconds between repeated volume steps while Left / Right stays held.")]
        [SerializeField] private float repeatInterval = 0.06f;

        private readonly PauseMenu menu = new PauseMenu();
        private KeyRepeat leftRepeat;
        private KeyRepeat rightRepeat;

        private InputActionMap puzzleMap;
        private InputAction walkLeft;
        private InputAction walkRight;
        private InputAction descend;
        private InputAction nextRow;
        private InputAction shiftLeft;
        private InputAction shiftRight;
        private InputAction switchRobot;
        private InputAction undo;
        private InputAction restart;
        private InputAction next;
        private InputAction up;
        private InputAction back;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Assets/Plugins/WebGL/BrowserKeys.jslib: keeps Tab (next row) from moving browser focus out of the game.
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void BlockBrowserTab();
#endif

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BlockBrowserTab();
#endif
            puzzleMap = InputSystem.actions.FindActionMap("Puzzle", true);
            walkLeft = puzzleMap.FindAction("WalkLeft", true);
            walkRight = puzzleMap.FindAction("WalkRight", true);
            descend = puzzleMap.FindAction("Descend", true);
            nextRow = puzzleMap.FindAction("NextRow", true);
            shiftLeft = puzzleMap.FindAction("ShiftLeft", true);
            shiftRight = puzzleMap.FindAction("ShiftRight", true);
            switchRobot = puzzleMap.FindAction("SwitchRobot", true);
            undo = puzzleMap.FindAction("Undo", true);
            restart = puzzleMap.FindAction("Restart", true);
            next = puzzleMap.FindAction("Next", true);
            up = puzzleMap.FindAction("Up", true);
            back = puzzleMap.FindAction("Back", true);
            leftRepeat = new KeyRepeat(repeatDelay, repeatInterval);
            rightRepeat = new KeyRepeat(repeatDelay, repeatInterval);
        }

        private void OnEnable()
        {
            puzzleMap.Enable();
        }

        private void OnDisable()
        {
            puzzleMap.Disable();
        }

        private void Update()
        {
            if (stageManager.IsOnTitle)
            {
                if (next.WasPressedThisFrame()) stageManager.StartFromTitle();
                return;
            }

            // Stage select: arrows move the cursor, Enter plays, Esc / Backspace back to the title.
            if (stageManager.IsSelecting)
            {
                if (back.WasPressedThisFrame()) stageManager.Back();
                if (walkLeft.WasPressedThisFrame()) stageManager.MoveSelection(-1);
                if (walkRight.WasPressedThisFrame()) stageManager.MoveSelection(1);
                if (up.WasPressedThisFrame()) stageManager.MoveSelection(-1);
                if (descend.WasPressedThisFrame()) stageManager.MoveSelection(1);
                if (next.WasPressedThisFrame()) stageManager.PlaySelected();
                return;
            }

            if (menu.IsOpen)
            {
                UpdateMenu();
                return;
            }

            if (back.WasPressedThisFrame())
            {
                if (stageManager.CanPause)
                {
                    menu.Open();
                    // An arrow still held from the puzzle must not change the volume; it counts from its next press.
                    leftRepeat.IgnoreUntilReleased(walkLeft.IsPressed());
                    rightRepeat.IgnoreUntilReleased(walkRight.IsPressed());
                    ShowMenu();
                    return;
                }

                stageManager.Back();
            }

            // Shift + arrow shifts the row; the bare arrow binding also fires then, so walking waits for Shift to be up.
            bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            if (!shiftHeld && walkLeft.WasPressedThisFrame()) stageManager.Walk(-1);
            if (!shiftHeld && walkRight.WasPressedThisFrame()) stageManager.Walk(1);
            if (descend.WasPressedThisFrame()) stageManager.Descend();
            if (nextRow.WasPressedThisFrame()) stageManager.SelectNextRow();
            if (shiftLeft.WasPressedThisFrame()) stageManager.Shift(-1);
            if (shiftRight.WasPressedThisFrame()) stageManager.Shift(1);
            if (switchRobot.WasPressedThisFrame()) stageManager.SwitchRobot();
            if (undo.WasPressedThisFrame()) stageManager.Undo();
            if (restart.WasPressedThisFrame()) stageManager.Restart();
            if (next.WasPressedThisFrame()) stageManager.Next();
        }

        // Pause menu: Up / Down move the cursor, Left / Right (held repeats, Shift ignored) change the BGM or SFX level,
        // Enter runs RESUME or STAGES, Esc / Backspace resumes. Returns before the puzzle sees any key this frame.
        private void UpdateMenu()
        {
            if (back.WasPressedThisFrame())
            {
                CloseMenu();
                return;
            }

            if (next.WasPressedThisFrame() && menu.Selected >= PauseItem.Resume)
            {
                bool toStages = menu.Selected == PauseItem.Stages;
                CloseMenu();
                if (toStages)
                {
                    stageManager.Back();
                }

                return;
            }

            bool moved = up.WasPressedThisFrame() || descend.WasPressedThisFrame();
            if (up.WasPressedThisFrame()) menu.Move(-1);
            if (descend.WasPressedThisFrame()) menu.Move(1);
            int steps = rightRepeat.Tick(walkRight.IsPressed(), Time.unscaledDeltaTime) - leftRepeat.Tick(walkLeft.IsPressed(), Time.unscaledDeltaTime);
            if (steps != 0 && menu.Selected == PauseItem.Bgm) sound.ChangeBgm(steps);
            if (steps != 0 && menu.Selected == PauseItem.Sfx) sound.ChangeSfx(steps);
            // Redraw only on a change: the view builds row strings.
            if (moved || steps != 0)
            {
                ShowMenu();
            }
        }

        private void ShowMenu()
        {
            pauseView.Show(menu.Selected, sound.BgmLevel, sound.SfxLevel);
        }

        private void CloseMenu()
        {
            menu.Close();
            pauseView.Hide();
        }
    }
}
