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
            Quaternion camRot = _mainCamera.transform.rotation;

            Vector3 current = transform.rotation.eulerAngles;
            Vector3 target = camRot.eulerAngles;

            // Smooth Y axis
            float smoothYaw = Mathf.LerpAngle(current.y, target.y, Time.deltaTime * 50f);

            float pitch = target.x;
            float roll = target.z;

            transform.rotation = Quaternion.Euler(pitch, smoothYaw, roll);
        }

        if (centeredBillboard && centerTransform != null) {
            transform.position = centerTransform.position + offset;
        }
    }
}
