using ProjectShift.Audio;
using ProjectShift.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectShift.Input
{
    /// <summary>
    /// Maps the project-wide "Puzzle" action map to stage actions: walk, descend, select a row, shift it, switch robot, undo, restart, next stage, plus mute.
    /// Bindings live in Assets/InputSystem_Actions.inputactions.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] private StageManager stageManager;
        [SerializeField] private SoundPlayer sound;

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
        private InputAction mute;
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
            mute = puzzleMap.FindAction("Mute", true);
            up = puzzleMap.FindAction("Up", true);
            back = puzzleMap.FindAction("Back", true);
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
            if (mute.WasPressedThisFrame()) sound.ToggleMute();

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

            if (back.WasPressedThisFrame()) stageManager.Back();
            if (walkLeft.WasPressedThisFrame()) stageManager.Walk(-1);
            if (walkRight.WasPressedThisFrame()) stageManager.Walk(1);
            if (descend.WasPressedThisFrame()) stageManager.Descend();
            if (nextRow.WasPressedThisFrame()) stageManager.SelectNextRow();
            if (shiftLeft.WasPressedThisFrame()) stageManager.Shift(-1);
            if (shiftRight.WasPressedThisFrame()) stageManager.Shift(1);
            if (switchRobot.WasPressedThisFrame()) stageManager.SwitchRobot();
            if (undo.WasPressedThisFrame()) stageManager.Undo();
            if (restart.WasPressedThisFrame()) stageManager.Restart();
            if (next.WasPressedThisFrame()) stageManager.Next();
        }
    }
}
