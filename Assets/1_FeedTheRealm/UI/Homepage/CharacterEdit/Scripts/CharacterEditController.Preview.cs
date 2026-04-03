using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterEditController
{
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        logger.Log("Geometry changed.", this);
        centerCharacterPreview();
    }

    private void centerCharacterPreview()
    {
        if (_characterPreview == null || canvasCharacterPreview == null)
            return;

        var previewParent = canvasCharacterPreview.parent as RectTransform;
        if (previewParent == null)
            return;

        previewParent.localScale = Vector3.one;

        var rect = _characterPreview.worldBound;

        // Convert preview bounds from screen space to parent-local space to get accurate size.
        Vector2 topLeftScreen = new Vector2(rect.xMin, Screen.height - rect.yMin);
        Vector2 bottomRightScreen = new Vector2(rect.xMax, Screen.height - rect.yMax);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewParent,
            topLeftScreen,
            null,
            out Vector2 topLeftLocal
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewParent,
            bottomRightScreen,
            null,
            out Vector2 bottomRightLocal
        );

        float localWidth = Mathf.Abs(bottomRightLocal.x - topLeftLocal.x);
        float localHeight = Mathf.Abs(topLeftLocal.y - bottomRightLocal.y);
        float squareSize = Mathf.Min(localWidth, localHeight);
        squareSize *= Mathf.Clamp(characterPreviewFillRatio, 0.2f, 1.25f);

        if (squareSize <= 0f)
        {
            return;
        }

        canvasCharacterPreview.localScale = Vector3.one;
        canvasCharacterPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, squareSize);
        canvasCharacterPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, squareSize);

        // Get screen center of the UI Toolkit element (Toolkit origin = top-left)
        Vector2 screenCenter = new Vector2(
            rect.xMin + rect.width / 2f,
            rect.yMin + rect.height / 2f
        );

        // Convert Y from top-left to bottom-left origin
        screenCenter.y = Screen.height - screenCenter.y;

        // Convert screen point to local position within the canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewParent,
            screenCenter,
            null, // camera if Canvas = Screen Space - Overlay
            out Vector2 localPoint
        );

        canvasCharacterPreview.anchoredPosition = localPoint + characterInContainerOffset;
    }

    private void ShowToastSuccess(string message)
    {
        ToastNotification.Show(message, "success", Color.green);
    }

    private void ShowToastError(string message)
    {
        ToastNotification.Show(message, "error", Color.red);
    }
}
