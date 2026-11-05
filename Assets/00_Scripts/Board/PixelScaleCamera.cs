using UnityEngine;

namespace ProjectShift.Board
{
    /// <summary>
    /// Draws one art pixel as a whole number of screen pixels: picks the largest integer scale at which the 640x360 art
    /// fits the window and sets the orthographic size to match. The space left over shows the camera background, which is
    /// the letterbox (it follows the clear fade because it is the same colour).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelScaleCamera : MonoBehaviour
    {
        private const int ArtWidth = 640;
        private const int ArtHeight = 360;

        private Camera cam;
        private Transform cachedTransform;
        private Vector3 basePosition;
        private int shownWidth;
        private int shownHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cachedTransform = transform;
            basePosition = cachedTransform.position;
        }

        private void LateUpdate()
        {
            if (Screen.width == shownWidth && Screen.height == shownHeight)
            {
                return;
            }

            shownWidth = Screen.width;
            shownHeight = Screen.height;
            // Below 640x360 the art is cropped rather than drawn at a fraction of a pixel.
            int scale = Mathf.Max(1, Mathf.Min(shownWidth / ArtWidth, shownHeight / ArtHeight));
            float unitsPerScreenPixel = 1f / (BoardView.PixelsPerUnit * scale);
            cam.orthographicSize = shownHeight * 0.5f * unitsPerScreenPixel;

            // With an odd width or height the view centre falls mid-pixel; shift half a screen pixel so art pixel edges
            // land on screen pixel edges.
            Vector3 offset = new Vector3(shownWidth % 2, shownHeight % 2, 0f) * (0.5f * unitsPerScreenPixel);
            cachedTransform.position = basePosition + offset;
        }
    }
}
