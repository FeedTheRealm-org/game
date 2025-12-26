using UnityEngine;
using UnityEngine.UIElements;

public class UIDialogController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    // Containers
    private VisualElement _root;

    void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        // _root.style.display = DisplayStyle.None;
    }

    private void OnEnable() { }

    private void OnDisable() { }
}
