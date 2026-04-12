using FTR.Core.Common.Characters;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.WorldSpace
{
    /// <summary>
    /// Controller that shows character name in world-space.
    /// <summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorldCharacterNameUI : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        private VisualElement _root;
        private Label _nameLabel;
        private ICharacterIdentity _identity;

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

            _identity = GetComponentInParent<ICharacterIdentity>();
            if (_identity == null)
            {
                logger.Log("ICharacterIdentity not found in parent.", this, Logging.LogType.Error);
                return;
            }

            if (_identity.IsLocalPlayer)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _identity.OnCharacterNameChanged += OnCharacterNameChanged;

            OnCharacterNameChanged(_identity.CharacterName);
        }

        private void OnDestroy()
        {
            if (_identity != null)
                _identity.OnCharacterNameChanged -= OnCharacterNameChanged;
        }

        public void SetName(string characterName)
        {
            if (_nameLabel != null)
                _nameLabel.text = characterName;
        }

        private void OnCharacterNameChanged(string newName)
        {
            if (_nameLabel == null)
                return;

            _nameLabel.text = newName;

            _root.style.display = string.IsNullOrEmpty(newName)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
