using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterEditController
{
    private Camera GetPreviewCanvasEventCamera(RectTransform referenceRect)
    {
        var canvas = referenceRect.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }

    private bool TryGetPreviewBoundsInScreenSpace(out Rect previewBoundsScreen)
    {
        previewBoundsScreen = default;
        if (_characterPreview == null || _root == null || _root.panel == null)
            return false;

        var previewBoundsPanel = _characterPreview.worldBound;
        var panelBounds = _root.panel.visualTree.worldBound;

        if (previewBoundsPanel.width <= 0f || previewBoundsPanel.height <= 0f)
            return false;

        if (panelBounds.width <= 0f || panelBounds.height <= 0f)
            return false;

        float screenXMin =
            ((previewBoundsPanel.xMin - panelBounds.xMin) / panelBounds.width) * Screen.width;
        float screenXMax =
            ((previewBoundsPanel.xMax - panelBounds.xMin) / panelBounds.width) * Screen.width;
        float screenYMinTopLeft =
            ((previewBoundsPanel.yMin - panelBounds.yMin) / panelBounds.height) * Screen.height;
        float screenYMaxTopLeft =
            ((previewBoundsPanel.yMax - panelBounds.yMin) / panelBounds.height) * Screen.height;

        float screenYTop = Screen.height - screenYMinTopLeft;
        float screenYBottom = Screen.height - screenYMaxTopLeft;

        previewBoundsScreen = Rect.MinMaxRect(
            Mathf.Min(screenXMin, screenXMax),
            Mathf.Min(screenYBottom, screenYTop),
            Mathf.Max(screenXMin, screenXMax),
            Mathf.Max(screenYBottom, screenYTop)
        );
        return true;
    }

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

        if (!TryGetPreviewBoundsInScreenSpace(out var previewBoundsScreen))
            return;

        var eventCamera = GetPreviewCanvasEventCamera(previewParent);

        previewParent.localScale = Vector3.one;

        // Convert preview bounds from screen space to parent-local space to get accurate size.
        Vector2 topLeftScreen = new Vector2(previewBoundsScreen.xMin, previewBoundsScreen.yMax);
        Vector2 bottomRightScreen = new Vector2(previewBoundsScreen.xMax, previewBoundsScreen.yMin);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewParent,
            topLeftScreen,
            eventCamera,
            out Vector2 topLeftLocal
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewParent,
            bottomRightScreen,
            eventCamera,
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

        Vector2 screenCenter = previewBoundsScreen.center;

        // Convert screen point to local position within the canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewParent,
            screenCenter,
            eventCamera,
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
