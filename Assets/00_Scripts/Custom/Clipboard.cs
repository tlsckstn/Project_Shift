using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectShift.Custom
{
    /// <summary>
    /// Copy and paste of one short text (a stage code). WebGL goes through the browser (Assets/Plugins/WebGL/Clipboard.jslib);
    /// the Editor and standalone players use the system clipboard.
    /// </summary>
    public static class Clipboard
    {
        /// <summary>Longest text <see cref="TakePasted"/> returns; the rest is cut off.</summary>
        public const int MaxLength = 4096;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ClipboardInit(int maxLength);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ClipboardCopy(string text);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string ClipboardTakePasted();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ClipboardRequestPaste();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int ClipboardTakeReadBlocked();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            ClipboardInit(MaxLength);
        }
#else
        // Text a RequestPaste read, until TakePasted hands it over.
        private static string requested;
#endif

        /// <summary>
        /// Puts <paramref name="text"/> on the clipboard. On WebGL, call it in the frame of the key press that asks for the copy:
        /// the browser only allows a copy shortly after a user action.
        /// </summary>
        public static void Copy(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ClipboardCopy(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        /// <summary>
        /// Reads the clipboard for a paste button; the text arrives through <see cref="TakePasted"/>, on WebGL a few frames later.
        /// Call it in the frame of the key press. When the browser refuses the read, <see cref="TakeReadBlocked"/> turns true and
        /// the screen asks for Ctrl+V instead.
        /// </summary>
        public static void RequestPaste()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ClipboardRequestPaste();
#else
            requested = Cap(GUIUtility.systemCopyBuffer ?? string.Empty);
#endif
        }

        /// <summary>True once after a <see cref="RequestPaste"/> the browser refused (no permission, or an iframe that blocks it).</summary>
        public static bool TakeReadBlocked()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return ClipboardTakeReadBlocked() != 0;
#else
            return false;
#endif
        }

        /// <summary>
        /// The text pasted since the last call (Ctrl+V, or a <see cref="RequestPaste"/> read), or null when nothing was. An empty
        /// clipboard comes back as an empty string. A screen that waits for a paste calls it once when it opens, to drop text
        /// pasted earlier, then every frame.
        /// </summary>
        public static string TakePasted()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return ClipboardTakePasted();
#else
            if (requested != null)
            {
                string read = requested;
                requested = null;
                return read;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.ctrlKey.isPressed || !keyboard.vKey.wasPressedThisFrame)
            {
                return null;
            }

            string text = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return Cap(text);
#endif
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private static string Cap(string text)
        {
            return text.Length > MaxLength ? text.Substring(0, MaxLength) : text;
        }
#endif
    }
}
