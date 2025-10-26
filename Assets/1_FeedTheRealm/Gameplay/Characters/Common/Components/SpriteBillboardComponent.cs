using UnityEngine;

public class SpriteBillboardComponent : MonoBehaviour {
    [Header("Centered billboard settings")]
    [SerializeField]
    private bool centeredBillboard = false;

    [SerializeField]
    private Transform centerTransform;

    [SerializeField]
    private Vector3 offset = new Vector3(0, 1f, 0);

    private Camera _mainCamera;


    private void Start() {
        _mainCamera = Camera.main;
    }

    private void LateUpdate() {
        if (_mainCamera == null) {
            _mainCamera = Camera.main;
        } else {
            transform.rotation = _mainCamera.transform.rotation;
        }
      
        if (centeredBillboard && centerTransform != null) {
              transform.position = centerTransform.position + offset;
        }
    }
}
