using UnityEngine;

public class SpriteBillboardComponent : MonoBehaviour
{
    private Camera _mainCamera;


    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        transform.rotation = _mainCamera.transform.rotation;
    }
}
