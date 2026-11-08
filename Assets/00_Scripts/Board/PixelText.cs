using TMPro;
using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>Silkscreen pixel-font helpers shared by the board and the stage select.</summary>
    internal static class PixelText
    {
        /// <summary>
        /// Small tier. Silkscreen is an 8 px em font; TMP draws fontSize 10 at 1 unit per em (measured), so this size makes one
        /// font pixel one art pixel. Only integer multiples keep the point-filtered glyphs sharp.
        /// </summary>
        public const float FontSize = 10f * 8f / BoardView.PixelsPerUnit;

        /// <summary>Large tier (changing values: row values, the shifts line, messages and headings).</summary>
        public const float LargeFontSize = 2f * FontSize;

        // 0-3 stars (art-src/gen/keycaps.py ICONS), indexed by star count.
        private static readonly string[] StarTexts = { "", Key("Star"), Key("Star") + Key("Star"), Key("Star") + Key("Star") + Key("Star") };

        /// <summary>
        /// Moves a label to localPosition, then nudges it so the first glyph's origin and the baseline sit on the art
        /// pixel grid: centred or right-aligned text can start on a half pixel, and the point-filtered pixel font smears.
        /// Every advance is whole pixels, so the rest of the line follows. Call after every SetText or move.
        /// </summary>
        public static void Place(TextMeshPro label, Vector3 localPosition)
        {
            label.rectTransform.localPosition = localPosition;
            label.ForceMeshUpdate();
            if (label.textInfo.characterCount == 0)
            {
                return;
            }

            TMP_CharacterInfo first = label.textInfo.characterInfo[0];
            Vector3 world = label.transform.TransformPoint(new Vector3(first.origin, first.baseLine, 0f));
            float dx = Mathf.Round(world.x * BoardView.PixelsPerUnit) / BoardView.PixelsPerUnit - world.x;
            float dy = Mathf.Round(world.y * BoardView.PixelsPerUnit) / BoardView.PixelsPerUnit - world.y;
            label.transform.position += new Vector3(dx, dy, 0f);
        }

        /// <summary>Inline key cap sprite tag; tint=1 makes the white cap take the label colour.</summary>
        public static string Key(string spriteName)
        {
            return "<sprite name=\"" + spriteName + "\" tint=1>";
        }

        /// <summary>Star key caps for 0-3 stars.</summary>
        public static string Stars(int count)
        {
            return StarTexts[count];
        }

        /// <summary>A centred small-tier label <paramref name="width"/> units wide, one unit high.</summary>
        public static TextMeshPro CreateLabel(string objectName, Transform parent, TMP_FontAsset font, float width)
        {
            TextMeshPro label = new GameObject(objectName).AddComponent<TextMeshPro>();
            label.rectTransform.SetParent(parent, false);
            label.rectTransform.sizeDelta = new Vector2(width, 1f);
            label.font = font;
            label.fontSize = FontSize;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }
    }
}
