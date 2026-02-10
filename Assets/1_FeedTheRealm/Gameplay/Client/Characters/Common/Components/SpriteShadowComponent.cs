using UnityEngine;

public class SpriteShadowComponent : MonoBehaviour
{
    private SpriteRenderer _sr;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        else
        {
            Debug.LogError(
                "Sprite shadow component used in game object with no sprite renderer: "
                    + gameObject.name
            );
        }
    }
}
