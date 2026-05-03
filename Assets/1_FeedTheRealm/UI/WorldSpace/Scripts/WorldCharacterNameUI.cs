using FTR.Core.Common.Characters;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.WorldSpace
{
    /// <summary>
    /// Controller that shows character name in world-space.
    /// <summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorldCharacterNameUI : MonoBehaviour, ICharacterNameController
    {
        [SerializeField]
        private Logging.Logger logger;

        private VisualElement _root;
        private Label _nameLabel;
        private string _pendingName;
        private bool _isInitialized;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _nameLabel = _root.Q<Label>("CharacterNameLabel");

            if (_nameLabel == null)
            {
                logger.Log(
                    "CharacterNameLabel not found in UIDocument.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            var identity = GetComponentInParent<ICharacterIdentity>();
            if (identity != null && identity.IsLocalPlayer)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _isInitialized = true;

            if (_pendingName != null)
            {
                SetName(_pendingName);
            }
            else
            {
                _root.style.display = DisplayStyle.None; // Hide initially until name is set
            }
        }

        private void OnDestroy() { }

        public void SetName(string characterName)
        {
            if (!_isInitialized)
            {
                _pendingName = characterName;
                return;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = characterName;
                _root.style.display = string.IsNullOrEmpty(characterName)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }
    }
}
