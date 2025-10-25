using UnityEngine;

public class SpriteBillboardComponent : MonoBehaviour {
    private Camera _mainCamera;


    private void Start() {
        _mainCamera = Camera.main;
    }

    private void LateUpdate() {
        if (_mainCamera == null) {
            _mainCamera = Camera.main;
        }
        if (_mainCamera != null) {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }
}
