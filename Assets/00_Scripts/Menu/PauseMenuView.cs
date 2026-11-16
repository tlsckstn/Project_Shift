using ProjectShift.Audio;
using ProjectShift.Board;
using TMPro;
using UnityEngine;

namespace ProjectShift.Menu
{
    /// <summary>
    /// Draws the PAUSED panel over a dimmed board: BGM and SFX as 8-LED bars, then RESUME and STAGES, and a key line.
    /// The sound-only panel (title, stage select) is a shorter panel headed SOUND with only BGM and SFX.
    /// Built on the first Show and reused. Layout numbers are art px from the screen centre, y up.
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        private const float ScreenWidth = 640f;
        private const float ScreenHeight = 360f;
        private const float PanelWidth = 240f;
        private const float PanelHeight = 132f;
        // Without the divider, RESUME and STAGES (42 px): the title and the volume rows move down, the key line up, by half.
        private const float SoundPanelHeight = 90f;
        private const float TitleY = 46f;
        private const float KeysY = -56f;
        private const float RowX = -104f;
        // Row names start right of the cursor column, so the cursor never moves them.
        private const float NameX = -94f;
        private const float LedX = 104f;
        // The Led glyph sits higher than the letters on the same baseline (measured 3 art px).
        private const float LedDropY = -3f;
        private const float LabelWidth = 8f;
        // Above the board, its decor and the key guide (sorting orders up to 6).
        private const int DimOrder = 20;
        private const int PanelOrder = 21;
        private const int TextOrder = 23;
        private static readonly float[] RowY = { 22f, 8f, -20f, -34f };
        private static readonly string[] RowNames = { "BGM", "SFX", "RESUME", "STAGES" };
        private static readonly string KeysText =
            PixelText.Key("Up") + PixelText.Key("Down") + " SELECT  " + PixelText.Key("Left") + PixelText.Key("Right") + " VOLUME  " +
            PixelText.Key("Enter") + " OK  " + PixelText.Key("Esc") + " RESUME";
        private static readonly string SoundKeysText =
            PixelText.Key("Up") + PixelText.Key("Down") + " SELECT  " + PixelText.Key("Left") + PixelText.Key("Right") + " VOLUME  " +
            PixelText.Key("Esc") + " BACK";

        [Tooltip("Silkscreen bitmap font asset (Assets/01_Art/Fonts).")]
        [SerializeField] private TMP_FontAsset pixelFont;
        [Tooltip("Key cap sprites; the volume LEDs are the Led cap.")]
        [SerializeField] private TMP_SpriteAsset keyCapSprites;
        [Tooltip("1x1 white sprite, bottom-left pivot, for the dim layer and the panel.")]
        [SerializeField] private Sprite pixelSprite;
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color panelColor = new Color32(0x23, 0x2A, 0x38, 0xFF);
        [SerializeField] private Color frameColor = new Color32(0x5C, 0x66, 0x78, 0xFF);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color itemColor = new Color32(0xA6, 0xAE, 0xBD, 0xFF);
        [SerializeField] private Color selectedColor = new Color32(0xE6, 0xCC, 0x33, 0xFF);

        private Transform root;
        private Transform frame;
        private Transform panel;
        private TextMeshPro divider;
        private TextMeshPro title;
        private TextMeshPro keys;
        private TextMeshPro[] rows;
        private TextMeshPro cursor;
        private TextMeshPro bgmLeds;
        private TextMeshPro sfxLeds;
        private int shownBgm = -1;
        private int shownSfx = -1;
        // null until the first Show, so the first one always lays out the mode.
        private bool? shownSoundOnly;
        // How far the title and rows sit below their PAUSED place (0 on the PAUSED panel).
        private float rowShift;

        /// <summary>
        /// Shows the panel with the cursor on <paramref name="selected"/> and the two volume levels (0-8) as LEDs;
        /// <paramref name="soundOnly"/> shows the shorter SOUND panel with only BGM and SFX.
        /// </summary>
        public void Show(PauseItem selected, int bgmLevel, int sfxLevel, bool soundOnly)
        {
            // Active before building and placing: TMP only lays out (and PixelText.Place only snaps) active text.
            if (root == null)
            {
                Build();
            }

            root.gameObject.SetActive(true);
            if (shownSoundOnly != soundOnly)
            {
                shownSoundOnly = soundOnly;
                LayOut(soundOnly);
                // The LED bars move with the rows: place them again below.
                shownBgm = -1;
                shownSfx = -1;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].color = i == (int)selected ? selectedColor : itemColor;
            }

            PixelText.Place(cursor, Pixels(RowX, RowY[(int)selected] - rowShift));

            if (bgmLevel != shownBgm)
            {
                shownBgm = bgmLevel;
                bgmLeds.SetText(PixelText.LedBar(bgmLevel, VolumeLevels.Max));
                PixelText.Place(bgmLeds, Pixels(LedX, RowY[(int)PauseItem.Bgm] - rowShift + LedDropY));
            }

            if (sfxLevel != shownSfx)
            {
                shownSfx = sfxLevel;
                sfxLeds.SetText(PixelText.LedBar(sfxLevel, VolumeLevels.Max));
                PixelText.Place(sfxLeds, Pixels(LedX, RowY[(int)PauseItem.Sfx] - rowShift + LedDropY));
            }
        }

        // PAUSED: full panel, every row and the divider. SOUND: shorter panel, BGM and SFX only, centred the same way.
        private void LayOut(bool soundOnly)
        {
            float height = soundOnly ? SoundPanelHeight : PanelHeight;
            rowShift = (PanelHeight - height) / 2f;
            SetRect(frame, -PanelWidth / 2f, -height / 2f, PanelWidth, height);
            SetRect(panel, -PanelWidth / 2f + 1f, -height / 2f + 1f, PanelWidth - 2f, height - 2f);

            title.SetText(soundOnly ? "SOUND" : "PAUSED");
            PixelText.Place(title, Pixels(0f, TitleY - rowShift));
            keys.SetText(soundOnly ? SoundKeysText : KeysText);
            PixelText.Place(keys, Pixels(0f, KeysY + rowShift));
            divider.gameObject.SetActive(!soundOnly);
            for (int i = 0; i < rows.Length; i++)
            {
                bool shown = !soundOnly || i <= (int)PauseItem.Sfx;
                rows[i].gameObject.SetActive(shown);
                if (shown)
                {
                    PixelText.Place(rows[i], Pixels(NameX, RowY[i] - rowShift));
                }
            }
        }

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void Build()
        {
            root = new GameObject("PauseMenu").transform;
            root.SetParent(transform, false);
            CreateRect("Dim", -ScreenWidth / 2f, -ScreenHeight / 2f, ScreenWidth, ScreenHeight, dimColor, DimOrder);
            // Frame and panel are sized per mode in LayOut.
            frame = CreateRect("Frame", 0f, 0f, 1f, 1f, frameColor, PanelOrder);
            panel = CreateRect("Panel", 0f, 0f, 1f, 1f, panelColor, PanelOrder + 1);

            title = CreateLabel("PauseTitle", titleColor, TextAlignmentOptions.Center);
            title.fontSize = PixelText.LargeFontSize;

            divider = CreateLabel("PauseDivider", frameColor, TextAlignmentOptions.Center);
            divider.SetText("- - - - - - - -");
            PixelText.Place(divider, Pixels(0f, -6f));

            keys = CreateLabel("PauseKeys", itemColor, TextAlignmentOptions.Center);
            keys.spriteAsset = keyCapSprites;

            rows = new TextMeshPro[RowNames.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreateLabel("PauseRow", itemColor, TextAlignmentOptions.Left);
                rows[i].SetText(RowNames[i]);
            }

            cursor = CreateLabel("PauseCursor", selectedColor, TextAlignmentOptions.Left);
            cursor.SetText(">");

            bgmLeds = CreateLabel("BgmLeds", Color.white, TextAlignmentOptions.Right);
            bgmLeds.spriteAsset = keyCapSprites;
            sfxLeds = CreateLabel("SfxLeds", Color.white, TextAlignmentOptions.Right);
            sfxLeds.spriteAsset = keyCapSprites;
        }

        private Transform CreateRect(string objectName, float x, float y, float width, float height, Color color, int sortingOrder)
        {
            GameObject rect = new GameObject(objectName);
            rect.transform.SetParent(root, false);
            SetRect(rect.transform, x, y, width, height);
            SpriteRenderer spriteRenderer = rect.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = pixelSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return rect.transform;
        }

        // The pixel sprite has a bottom-left pivot: (x, y) is the bottom-left corner, the scale its size in art px.
        private static void SetRect(Transform rect, float x, float y, float width, float height)
        {
            rect.localPosition = Pixels(x, y);
            rect.localScale = new Vector3(width, height, 1f);
        }

        // The pivot follows the alignment, so x is the left edge, centre or right edge of the text.
        private TextMeshPro CreateLabel(string objectName, Color color, TextAlignmentOptions alignment)
        {
            TextMeshPro label = PixelText.CreateLabel(objectName, root, pixelFont, LabelWidth);
            label.color = color;
            label.sortingOrder = TextOrder;
            label.alignment = alignment;
            float pivotX = alignment == TextAlignmentOptions.Left ? 0f : alignment == TextAlignmentOptions.Right ? 1f : 0.5f;
            label.rectTransform.pivot = new Vector2(pivotX, 0.5f);
            return label;
        }

        private static Vector3 Pixels(float x, float y)
        {
            return new Vector3(x, y, 0f) / BoardView.PixelsPerUnit;
        }
    }
}
