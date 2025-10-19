using UnityEngine;

public class SpriteBillboardComponent : MonoBehaviour {
    private void LateUpdate() {
        transform.rotation = Camera.main.transform.rotation;
    }
}
