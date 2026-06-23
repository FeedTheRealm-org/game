using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldSpaceUIDepthSorter : MonoBehaviour
{
    public string CharacterSortingLayer = "Characters";
    public int DepthSortingPrecision = 100;
    public int DepthSortingOffset;
    public Camera DepthSortingCamera;

    private Renderer _uiRenderer;

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (_uiRenderer == null)
        {
            _uiRenderer = GetComponent<Renderer>();
            if (_uiRenderer == null)
                return;
        }

        if (DepthSortingCamera == null)
        {
            if (Camera.main == null)
                return;
            DepthSortingCamera = Camera.main;
        }

        _uiRenderer.sortingLayerName = CharacterSortingLayer;

        var precision = DepthSortingPrecision <= 0 ? 100 : DepthSortingPrecision;
        var depth = Vector3.Dot(transform.position, DepthSortingCamera.transform.forward);
        _uiRenderer.sortingOrder = Mathf.RoundToInt(-depth * precision) + DepthSortingOffset;
    }
}
