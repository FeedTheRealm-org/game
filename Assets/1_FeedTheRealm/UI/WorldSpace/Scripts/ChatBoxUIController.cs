using FeedTheRealm.Core.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.WorldSpace
{
    /// <summary>
    /// Screen-space UI controller for the world chat message box.
    /// Displays the latest chat message and fades it out after a few seconds.
    /// </summary>
    public class ChatBoxUIController : MonoBehaviour, IChatBox
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private float displayDuration = 5f;

        [SerializeField]
        private float fadeDuration = 1f;

        private VisualElement _root;
        private Label _msgLabel;

        private Coroutine _fadeCoroutine;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _msgLabel = _root.Q<Label>("ChatText");

            if (_msgLabel == null)
                logger.Log(
                    "[ChatMessageUIController] Could not find required UI element: ChatText.",
                    this,
                    Logging.LogType.Error
                );

            _root.style.display = DisplayStyle.None;
        }

        public void ShowChatMessage(string message)
        {
            _msgLabel.text = message;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(ShowThenFade());
        }

        private System.Collections.IEnumerator ShowThenFade()
        {
            // Fully visible
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;

            yield return new WaitForSeconds(displayDuration);

            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _root.style.opacity = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            _root.style.opacity = 0f;
            _root.style.display = DisplayStyle.None;
        }
    }
}
