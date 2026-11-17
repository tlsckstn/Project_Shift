using ProjectShift.Audio;
using ProjectShift.Custom;
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
        [Tooltip("Stage select: seconds between repeated cursor steps while an arrow stays held (after Repeat Delay).")]
        [SerializeField] private float selectRepeatInterval = 0.1f;

        private readonly PauseMenu menu = new PauseMenu();
        private KeyRepeat leftRepeat;
        private KeyRepeat rightRepeat;
        // Stage select cursor: Left / Up step back, Right / Down step on.
        private KeyRepeat selectBackRepeat;
        private KeyRepeat selectOnRepeat;
        private bool wasSelecting;

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
            selectBackRepeat = new KeyRepeat(repeatDelay, selectRepeatInterval);
            selectOnRepeat = new KeyRepeat(repeatDelay, selectRepeatInterval);
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
            bool backHeld = walkLeft.IsPressed() || up.IsPressed();
            bool onHeld = walkRight.IsPressed() || descend.IsPressed();
            // An arrow still held from the puzzle must not slide the cursor; it counts from its next press.
            bool selecting = stageManager.IsSelecting;
            if (selecting && !wasSelecting)
            {
                selectBackRepeat.IgnoreUntilReleased(backHeld);
                selectOnRepeat.IgnoreUntilReleased(onHeld);
            }

            wasSelecting = selecting;

            if (menu.IsOpen)
            {
                UpdateMenu();
                return;
            }

            // P opens the sound panel (BGM, SFX) on the title and the stage select; the puzzle has it in its Esc menu.
            // Read straight from the keyboard: the "Puzzle" map has no P action and only these two screens use it.
            bool soundPressed = Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
            if (soundPressed && stageManager.CanOpenSound)
            {
                OpenMenu(true);
                return;
            }

            if (stageManager.IsOnTitle)
            {
                if (next.WasPressedThisFrame()) stageManager.StartFromTitle();
                // E opens the stage editor; read straight from the keyboard like P.
                else if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) stageManager.OpenEditor();
                return;
            }

            if (stageManager.IsEditing)
            {
                UpdateEditor();
                return;
            }

            // Stage select: arrows move the cursor (held repeats), Enter plays, Esc / Backspace back to the title.
            if (stageManager.IsSelecting)
            {
                if (back.WasPressedThisFrame()) stageManager.Back();
                int selectSteps = selectOnRepeat.Tick(onHeld, Time.unscaledDeltaTime) - selectBackRepeat.Tick(backHeld, Time.unscaledDeltaTime);
                for (int i = 0; i < Mathf.Abs(selectSteps); i++)
                {
                    stageManager.MoveSelection(selectSteps > 0 ? 1 : -1);
                }

                if (next.WasPressedThisFrame()) stageManager.PlaySelected();
                return;
            }

            if (back.WasPressedThisFrame())
            {
                if (stageManager.CanPause)
                {
                    OpenMenu(false);
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

        // Stage editor: arrows move the cell cursor, Space toggles a bit, 1-3 place robots (Shift: their exits), B bug, F firewall,
        // L locked row, N number lock (goal = the row's value now), C robot cover, - / = row count, , / . hours over par (verified board only), Esc / Backspace
        // back to the title. The editor keys are not in the "Puzzle" map, so they are read straight from the keyboard.
        private void UpdateEditor()
        {
            StageEditorView editorView = stageManager.Editor;
            // The verify panel takes every key: Esc cancels the check or closes the result, Enter closes a result.
            if (editorView.IsPanelOpen)
            {
                if (back.WasPressedThisFrame() || (next.WasPressedThisFrame() && editorView.HasResult)) editorView.ClosePanel();
                return;
            }

            if (back.WasPressedThisFrame())
            {
                stageManager.Back();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (!stageManager.CanEdit || keyboard == null)
            {
                return;
            }

            // Tab moves between the board and the button row; on the row, Left / Right pick and Enter presses.
            // Tab is read from the keyboard, not NextRow: NextRow is also bound to Space, which toggles a bit here.
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                sound.PlayWalk();
                editorView.ToggleFocus();
                return;
            }

            if (editorView.ButtonsFocused)
            {
                if (walkLeft.WasPressedThisFrame()) editorView.MoveButton(-1);
                else if (walkRight.WasPressedThisFrame()) editorView.MoveButton(1);
                else if (next.WasPressedThisFrame() && editorView.PressButton()) sound.PlayScreen();
                return;
            }

            StageEditor editor = editorView.Editor;
            bool shiftHeld = keyboard.shiftKey.isPressed;
            bool edited = true;
            if (walkLeft.WasPressedThisFrame()) editor.MoveCursor(0, -1);
            else if (walkRight.WasPressedThisFrame()) editor.MoveCursor(0, 1);
            else if (up.WasPressedThisFrame()) editor.MoveCursor(-1, 0);
            else if (descend.WasPressedThisFrame()) editor.MoveCursor(1, 0);
            else if (keyboard.spaceKey.wasPressedThisFrame) editor.ToggleBit();
            else if (keyboard.digit1Key.wasPressedThisFrame) PlaceRobotOrExit(editor, 0, shiftHeld);
            else if (keyboard.digit2Key.wasPressedThisFrame) PlaceRobotOrExit(editor, 1, shiftHeld);
            else if (keyboard.digit3Key.wasPressedThisFrame) PlaceRobotOrExit(editor, 2, shiftHeld);
            else if (keyboard.bKey.wasPressedThisFrame) editor.ToggleBug();
            else if (keyboard.fKey.wasPressedThisFrame) editor.ToggleFirewall();
            else if (keyboard.lKey.wasPressedThisFrame) editor.ToggleLockedRow();
            else if (keyboard.nKey.wasPressedThisFrame) editor.ToggleNumberLock();
            else if (keyboard.cKey.wasPressedThisFrame) editor.ToggleCover();
            else if (keyboard.minusKey.wasPressedThisFrame) editor.ChangeRowCount(-1);
            else if (keyboard.equalsKey.wasPressedThisFrame) editor.ChangeRowCount(1);
            else if (keyboard.commaKey.wasPressedThisFrame) edited = editorView.ChangeExtraHours(-1);
            else if (keyboard.periodKey.wasPressedThisFrame) edited = editorView.ChangeExtraHours(1);
            else edited = false;

            if (edited)
            {
                sound.PlayWalk();
                stageManager.Editor.Redraw();
            }
        }

        private static void PlaceRobotOrExit(StageEditor editor, int robot, bool exit)
        {
            if (exit)
            {
                editor.ToggleExit(robot);
            }
            else
            {
                editor.ToggleRobot(robot);
            }
        }

        // Pause menu: Up / Down move the cursor, Left / Right (held repeats, Shift ignored) change the BGM or SFX level,
        // Enter runs RESUME or STAGES (the sound panel has neither, so Enter does nothing there), Esc / Backspace closes,
        // P also closes the sound panel.
        // Returns before the screen behind sees any key this frame.
        private void UpdateMenu()
        {
            bool soundClose = menu.IsSoundOnly && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
            if (back.WasPressedThisFrame() || soundClose)
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

        private void OpenMenu(bool soundOnly)
        {
            menu.Open(soundOnly);
            // An arrow still held from the screen behind must not change the volume; it counts from its next press.
            leftRepeat.IgnoreUntilReleased(walkLeft.IsPressed());
            rightRepeat.IgnoreUntilReleased(walkRight.IsPressed());
            ShowMenu();
        }

        private void ShowMenu()
        {
            pauseView.Show(menu.Selected, sound.BgmLevel, sound.SfxLevel, menu.IsSoundOnly, stageManager.IsCustomPlay);
        }

        private void CloseMenu()
        {
            menu.Close();
            pauseView.Hide();
            // Same for the stage select cursor behind the sound panel: an arrow held on closing counts from its next press.
            selectBackRepeat.IgnoreUntilReleased(walkLeft.IsPressed() || up.IsPressed());
            selectOnRepeat.IgnoreUntilReleased(walkRight.IsPressed() || descend.IsPressed());
        }
    }
}
