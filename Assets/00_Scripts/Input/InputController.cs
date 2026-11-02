using ProjectShift.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectShift.Input
{
    /// <summary>
    /// Maps keys to stage actions: arrows walk and descend, W/S select a row, Q/E shift it, Z undo, R restart.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] private StageManager stageManager;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame) stageManager.Walk(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame) stageManager.Walk(1);
            if (keyboard.downArrowKey.wasPressedThisFrame) stageManager.Descend();
            if (keyboard.wKey.wasPressedThisFrame) stageManager.SelectRow(-1);
            if (keyboard.sKey.wasPressedThisFrame) stageManager.SelectRow(1);
            if (keyboard.qKey.wasPressedThisFrame) stageManager.Shift(-1);
            if (keyboard.eKey.wasPressedThisFrame) stageManager.Shift(1);
            if (keyboard.zKey.wasPressedThisFrame) stageManager.Undo();
            if (keyboard.rKey.wasPressedThisFrame) stageManager.Restart();
        }
    }
}
