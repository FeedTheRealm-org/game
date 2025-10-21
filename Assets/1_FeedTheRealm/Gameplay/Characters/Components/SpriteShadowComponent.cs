using UnityEngine;

public class SpriteShadowComponent : MonoBehaviour {
    void Start() {
        GetComponent<SpriteRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}
