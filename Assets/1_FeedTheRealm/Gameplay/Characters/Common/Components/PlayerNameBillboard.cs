using TMPro;
using UnityEngine;

/// <summary>
/// Simple world-space billboard for displaying a player name above a target.
/// Attach this as a child of the player (e.g. "NameTag" object) with a TextMeshPro component.
/// </summary>
public class PlayerNameBillboard : MonoBehaviour
{
    [Header("References")]
    // Use TMP_Text so it works with both TextMeshPro and TextMeshProUGUI
    [SerializeField]
    private TMP_Text textMesh;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private Camera targetCamera;

    [Header("Offset")]
    // World offset (used for world-space canvases or no canvas)
    [SerializeField]
    private Vector3 worldOffset = new Vector3(0f, 0f, 0f);

    // Extra pixel offset when rendering on a screen-space Canvas
    [SerializeField]
    private float screenOffsetX = 5f;

    [SerializeField]
    private float screenOffsetY = 40f;

    // Cached UI info (for Canvas-based nameplates)
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TMP_Text>();
        }
        // Try to get RectTransform/Canvas from self or from the text component
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (rectTransform == null && textMesh != null)
        {
            rectTransform = textMesh.rectTransform;
        }
        if (parentCanvas == null && textMesh != null)
        {
            parentCanvas = textMesh.canvas;
        }

        if (target == null && transform.parent != null)
        {
            target = transform.parent;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    public void SetName(string name)
    {
        if (textMesh != null)
        {
            textMesh.text = name;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Camera can be created after this object; try to grab it lazily
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }
        }

        // If we're inside a non-world-space Canvas (Screen Space), convert
        // the *target* world position to screen position and move the RectTransform.
        // Ignore worldOffset to avoid weird perspective effects; the
        // offset is done only in screen pixels.
        if (
            parentCanvas != null
            && parentCanvas.renderMode != RenderMode.WorldSpace
            && rectTransform != null
        )
        {
            Vector3 anchorWorldPos = target.position; // without worldOffset
            Vector3 screenPos = targetCamera.WorldToScreenPoint(anchorWorldPos);
            screenPos.x += screenOffsetX;
            screenPos.y += screenOffsetY; // vertical offset in screen pixels
            rectTransform.position = screenPos;
        }
        else
        {
            // World-space behaviour: here we do use worldOffset in world units
            Vector3 targetWorldPos = target.position + worldOffset;
            transform.position = targetWorldPos;
            transform.forward = targetCamera.transform.forward;
        }
    }
}
