using ProjectShift.Stage;
using TMPro;
using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Draws one stage select page as a computer-part zone (RAM, BUS, CPU CORES, ALU) with a minimap.
    /// Everything is built on the first Show and reused: later calls only change sprites, text, positions and active flags.
    /// Layout numbers are art px from the screen centre, y up, mirrored from the zone art (art-src/gen/zones.py).
    /// </summary>
    public class StageSelectView : MonoBehaviour
    {
        /// <summary>Most stages one stage select page (one zone) holds.</summary>
        public const int PageSize = 8;
        private const int Bits = 8;
        private const int CrewCount = 3;
        private const float HeadingY = 153f;
        private const float SublineY = 139f;
        private const float PageY = 125f;
        // Robot sprite centre above a slot's top edge: its tracks rest on the edge.
        private const float CursorLift = 9f;
        // Units; wide enough that the large heading never wraps.
        private const float LabelWidth = 16f;
        private const string StickLabel = "DDR-NS 8X1BIT DIMM_A1";

        [Tooltip("Silkscreen bitmap font asset (Assets/01_Art/Fonts).")]
        [SerializeField] private TMP_FontAsset pixelFont;
        [Tooltip("Key cap sprites; the stars are the Star cap.")]
        [SerializeField] private TMP_SpriteAsset keyCapSprites;
        [Tooltip("Per crew in robot order (blue, pink, yellow). The first is the cursor; CPU CORES shows all three.")]
        [SerializeField] private Sprite[] robotIdleSprites;
        [Tooltip("Static page art per zone (Ram, Bus, Cores, Alu), 640x360, placed at the screen centre.")]
        [SerializeField] private Sprite[] zoneSprites;
        [Tooltip("RAM chip per slot state (Locked, Open, Cleared).")]
        [SerializeField] private Sprite[] chipSprites;
        [Tooltip("BUS junction per slot state (Locked, Open, Cleared).")]
        [SerializeField] private Sprite[] junctionSprites;
        [Tooltip("CPU CORES block per slot state (Locked, Open, Cleared).")]
        [SerializeField] private Sprite[] blockSprites;
        [SerializeField] private Sprite bitOn;
        [SerializeField] private Sprite bitOff;
        [SerializeField] private Sprite bitLocked;
        [Tooltip("Motherboard minimap per current zone (Ram, Bus, Cores, Alu).")]
        [SerializeField] private Sprite[] minimapSprites;
        [Tooltip("1x1 white sprite, bottom-left pivot, for the lit bus traces.")]
        [SerializeField] private Sprite pixelSprite;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color sublineColor = new Color(0.55f, 0.6f, 0.68f);
        [SerializeField] private Color starColor = new Color(0.9f, 0.8f, 0.2f);
        [Tooltip("Stage number drawn on an open or cleared slot.")]
        [SerializeField] private Color numberOnSlotColor = new Color32(0x14, 0x1A, 0x26, 0xFF);
        [Tooltip("Lit bus traces, and the = n value of a cleared register (Green base).")]
        [SerializeField] private Color litTraceColor = new Color32(0x4D, 0xE6, 0x80, 0xFF);

        private Transform root;
        private SpriteRenderer zoneArt;
        private SpriteRenderer minimap;
        private TextMeshPro heading;
        private TextMeshPro subline;
        private TextMeshPro pageLabel;
        private TextMeshPro mapLabel;
        private TextMeshPro stickLabel;
        private SpriteRenderer[] slots;
        private TextMeshPro[] numbers;
        private TextMeshPro[] slotLabels;
        private TextMeshPro[] starLabels;
        private TextMeshPro[] valueLabels;
        private SpriteRenderer[] bits;
        // Per bus slot i: the lit trace to slot i + 1, along lane i, then down to lane i + 1.
        private Transform[] traceAcross;
        private Transform[] traceDown;
        private SpriteRenderer[] cursors;

        /// <summary>
        /// Shows one page: <paramref name="pageCount"/> stages from <paramref name="pageStart"/> (0-based, numbered from 1 on
        /// screen), drawn as the zone of 0-based <paramref name="chapter"/>, under <paramref name="title"/> and "page/pageTotal".
        /// Stages below <paramref name="unlocked"/> are open, cleared ones (stars, indexed by stage) lit; the cursor robot stands
        /// on <paramref name="selected"/>. <paramref name="firstLockTargets"/> (per stage, -1 without a lock) feeds the ALU bits.
        /// </summary>
        public void Show(int pageStart, int pageCount, string title, int page, int pageTotal, int chapter, int unlocked, int selected, int[] stars, int[] firstLockTargets)
        {
            // Active before building and placing: TMP only lays out (and PixelText.Place only snaps) active text.
            if (root == null)
            {
                Build();
            }

            root.gameObject.SetActive(true);
            Zone zone = ChapterZones.ZoneOf(chapter);
            zoneArt.sprite = zoneSprites[(int)zone];
            minimap.sprite = minimapSprites[(int)zone];
            SetText(heading, ChapterZones.Heading(title, zone), 0f, HeadingY, TextAlignmentOptions.Center);
            SetText(subline, ChapterZones.Subline(zone, pageCount), 0f, SublineY, TextAlignmentOptions.Center);
            pageLabel.SetText("{0}/{1}", page, pageTotal);
            PixelText.Place(pageLabel, Pixels(0f, PageY));
            PixelText.Place(mapLabel, Pixels(194f, -54f));
            stickLabel.gameObject.SetActive(zone == Zone.Ram);
            if (zone == Zone.Ram)
            {
                PixelText.Place(stickLabel, Pixels(-180f, 52f));
            }

            int selectedSlot = selected - pageStart;
            for (int i = 0; i < PageSize; i++)
            {
                ShowSlot(zone, i, i < pageCount, pageStart + i, unlocked, stars, firstLockTargets, i == selectedSlot);
                bool lit = zone == Zone.Bus && i + 1 < pageCount && ChapterZones.StateOf(pageStart + i, unlocked, stars[pageStart + i]) == SlotState.Cleared;
                traceAcross[i].gameObject.SetActive(lit);
                traceDown[i].gameObject.SetActive(lit);
            }

            Vector2 slot = SlotCentre(zone, selectedSlot);
            for (int c = 0; c < CrewCount; c++)
            {
                cursors[c].gameObject.SetActive(c == 0 || zone == Zone.Cores);
            }

            switch (zone)
            {
                case Zone.Ram:
                    cursors[0].transform.localPosition = Pixels(slot.x, slot.y + 20f + CursorLift);
                    break;
                case Zone.Bus:
                    cursors[0].transform.localPosition = Pixels(slot.x, slot.y + 8f + CursorLift);
                    break;
                case Zone.Cores:
                    for (int c = 0; c < CrewCount; c++)
                    {
                        cursors[c].transform.localPosition = Pixels(slot.x + 22f * (c - 1), slot.y);
                    }

                    break;
                default:
                    cursors[0].transform.localPosition = Pixels(-186f, slot.y);
                    break;
            }
        }

        /// <summary>Hides the stage select.</summary>
        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void ShowSlot(Zone zone, int i, bool shown, int stage, int unlocked, int[] stars, int[] firstLockTargets, bool selected)
        {
            bool alu = zone == Zone.Alu;
            slots[i].gameObject.SetActive(shown && !alu);
            numbers[i].gameObject.SetActive(shown);
            // BUS labels every lane, used or not.
            slotLabels[i].gameObject.SetActive(shown || zone == Zone.Bus);
            valueLabels[i].gameObject.SetActive(false);
            for (int b = 0; b < Bits; b++)
            {
                bits[i * Bits + b].gameObject.SetActive(shown && alu);
            }

            Vector2 centre = SlotCentre(zone, i);
            slotLabels[i].color = sublineColor;
            if (zone == Zone.Bus)
            {
                SetText(slotLabels[i], ChapterZones.SlotLabel(zone, i), -170f, centre.y, TextAlignmentOptions.Center);
            }

            int starCount = shown ? stars[stage] : 0;
            starLabels[i].gameObject.SetActive(starCount > 0);
            if (!shown)
            {
                return;
            }

            SlotState state = ChapterZones.StateOf(stage, unlocked, starCount);
            bool locked = state == SlotState.Locked;
            numbers[i].color = locked ? sublineColor : alu ? titleColor : numberOnSlotColor;
            numbers[i].SetText("{0:00}", stage + 1);
            string label = ChapterZones.SlotLabel(zone, i);
            string starText = PixelText.Stars(starCount);
            switch (zone)
            {
                case Zone.Ram:
                    slots[i].sprite = chipSprites[(int)state];
                    PlaceNumber(i, centre.x, centre.y, TextAlignmentOptions.Center);
                    SetText(slotLabels[i], label, centre.x, centre.y - 44f, TextAlignmentOptions.Center);
                    SetText(starLabels[i], starText, centre.x, centre.y - 30f, TextAlignmentOptions.Center);
                    break;
                case Zone.Bus:
                    slots[i].sprite = junctionSprites[(int)state];
                    PlaceNumber(i, centre.x, centre.y, TextAlignmentOptions.Center);
                    // Left edge 4 px past the junction: three stars centred at x + 22 would cover it.
                    SetText(starLabels[i], starText, centre.x + 16f, centre.y, TextAlignmentOptions.Left);
                    break;
                case Zone.Cores:
                    slots[i].sprite = blockSprites[(int)state];
                    // The selected block holds the three crew robots, so its number moves to the bottom-left corner.
                    if (selected)
                    {
                        PlaceNumber(i, centre.x - 31f, centre.y - 16f, TextAlignmentOptions.Left);
                    }
                    else
                    {
                        PlaceNumber(i, centre.x, centre.y, TextAlignmentOptions.Center);
                    }

                    slotLabels[i].color = numbers[i].color;
                    SetText(slotLabels[i], label, centre.x - 31f, centre.y + 16f, TextAlignmentOptions.Left);
                    SetText(starLabels[i], starText, centre.x + 31f, centre.y - 16f, TextAlignmentOptions.Right);
                    break;
                default:
                    int value = ChapterZones.RegisterValue(state, firstLockTargets[stage]);
                    for (int b = 0; b < Bits; b++)
                    {
                        // Leftmost bit is 128, as on the board.
                        bool on = value >= 0 && (value >> (Bits - 1 - b) & 1) == 1;
                        bits[i * Bits + b].sprite = locked ? bitLocked : on ? bitOn : bitOff;
                    }

                    PlaceNumber(i, -10f, centre.y, TextAlignmentOptions.Center);
                    SetText(slotLabels[i], label, -164f, centre.y, TextAlignmentOptions.Center);
                    SetText(starLabels[i], starText, 30f, centre.y, TextAlignmentOptions.Center);
                    valueLabels[i].gameObject.SetActive(value >= 0);
                    if (value >= 0)
                    {
                        valueLabels[i].SetText("= {0}", value);
                        PixelText.Place(valueLabels[i], Pixels(70f, centre.y));
                    }

                    break;
            }

            if (!alu)
            {
                slots[i].transform.localPosition = Pixels(centre.x, centre.y);
            }
        }

        private void PlaceNumber(int i, float x, float y, TextAlignmentOptions alignment)
        {
            Align(numbers[i], alignment);
            PixelText.Place(numbers[i], Pixels(x, y));
        }

        // Every SetText needs a fresh snap: a different width can put a centred label on a half pixel.
        private static void SetText(TextMeshPro label, string text, float x, float y, TextAlignmentOptions alignment)
        {
            if (!label.gameObject.activeSelf)
            {
                return;
            }

            Align(label, alignment);
            label.SetText(text);
            PixelText.Place(label, Pixels(x, y));
        }

        // The pivot follows the alignment, so x is the left edge, centre or right edge of the text.
        private static void Align(TextMeshPro label, TextAlignmentOptions alignment)
        {
            label.alignment = alignment;
            float pivotX = alignment == TextAlignmentOptions.Left ? 0f : alignment == TextAlignmentOptions.Right ? 1f : 0.5f;
            label.rectTransform.pivot = new Vector2(pivotX, 0.5f);
        }

        private static Vector2 SlotCentre(Zone zone, int i)
        {
            switch (zone)
            {
                case Zone.Ram: return new Vector2(-158f + 40f * i, 0f);
                case Zone.Bus: return new Vector2(-120f + 36f * i, 84f - 22f * i);
                case Zone.Cores: return new Vector2(-150f + 80f * (i % 4), 40f - 64f * (i / 4));
                default: return new Vector2(0f, 84f - 22f * i);
            }
        }

        private static Vector3 Pixels(float x, float y)
        {
            return new Vector3(x, y, 0f) / BoardView.PixelsPerUnit;
        }

        private void Build()
        {
            root = new GameObject("StageSelect").transform;
            root.SetParent(transform, false);
            // Above the circuit background (-10, -9), below everything else.
            zoneArt = CreateSprite("ZoneArt", -8, Vector3.zero);
            minimap = CreateSprite("Minimap", 0, Pixels(194f, -81f));
            heading = CreateLabel("ZoneHeading", titleColor, 0);
            heading.fontSize = PixelText.LargeFontSize;
            subline = CreateLabel("ZoneSubline", sublineColor, 0);
            pageLabel = CreateLabel("ZonePage", sublineColor, 0);
            mapLabel = CreateLabel("MapLabel", sublineColor, 0);
            mapLabel.SetText("MAP");
            stickLabel = CreateLabel("StickLabel", sublineColor, 0);
            Align(stickLabel, TextAlignmentOptions.Left);
            stickLabel.SetText(StickLabel);

            slots = new SpriteRenderer[PageSize];
            numbers = new TextMeshPro[PageSize];
            slotLabels = new TextMeshPro[PageSize];
            starLabels = new TextMeshPro[PageSize];
            valueLabels = new TextMeshPro[PageSize];
            bits = new SpriteRenderer[PageSize * Bits];
            traceAcross = new Transform[PageSize];
            traceDown = new Transform[PageSize];
            for (int i = 0; i < PageSize; i++)
            {
                slots[i] = CreateSprite("Slot", 2, Vector3.zero);
                numbers[i] = CreateLabel("StageNumber", sublineColor, 3);
                slotLabels[i] = CreateLabel("SlotLabel", sublineColor, 3);
                starLabels[i] = CreateLabel("StageStars", starColor, 3);
                starLabels[i].spriteAsset = keyCapSprites;
                valueLabels[i] = CreateLabel("RegisterValue", litTraceColor, 3);
                Align(valueLabels[i], TextAlignmentOptions.Left);
                Vector2 row = SlotCentre(Zone.Alu, i);
                for (int b = 0; b < Bits; b++)
                {
                    bits[i * Bits + b] = CreateSprite("Bit", 2, Pixels(-130f + 14f * b, row.y));
                }

                // Integer px rects; 2 px over the 1 px lane. Only the first PageSize - 1 slots have a next slot.
                Vector2 from = SlotCentre(Zone.Bus, i);
                Vector2 to = SlotCentre(Zone.Bus, i + 1);
                traceAcross[i] = CreateTrace(from.x, from.y - 1f, to.x + 1f - from.x, 2f);
                traceDown[i] = CreateTrace(to.x - 1f, to.y, 2f, from.y + 1f - to.y);
            }

            cursors = new SpriteRenderer[CrewCount];
            for (int c = 0; c < CrewCount; c++)
            {
                cursors[c] = CreateSprite("StageCursor", 5, Vector3.zero);
                cursors[c].sprite = robotIdleSprites[c];
            }
        }

        private Transform CreateTrace(float x, float y, float width, float height)
        {
            SpriteRenderer trace = CreateSprite("LitTrace", 1, Pixels(x, y));
            trace.sprite = pixelSprite;
            trace.color = litTraceColor;
            trace.transform.localScale = new Vector3(width, height, 1f);
            return trace.transform;
        }

        private SpriteRenderer CreateSprite(string objectName, int sortingOrder, Vector3 localPosition)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(root, false);
            spriteObject.transform.localPosition = localPosition;
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private TextMeshPro CreateLabel(string objectName, Color color, int sortingOrder)
        {
            TextMeshPro label = PixelText.CreateLabel(objectName, root, pixelFont, LabelWidth);
            label.color = color;
            label.sortingOrder = sortingOrder;
            return label;
        }
    }
}
