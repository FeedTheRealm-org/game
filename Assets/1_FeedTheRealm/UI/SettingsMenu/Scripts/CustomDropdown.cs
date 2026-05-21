using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Common
{
    [UxmlElement]
    public partial class CustomDropdown : VisualElement
    {
        public event Action<int> OnValueChanged;
        private Label _labelElement;
        private Button _button;
        private VisualElement _menu;
        private ScrollView _list;
        private VisualElement _arrowIcon;

        private List<string> _choices = new();
        private int _selectedIndex = 0;
        private bool _isOpen = false;

        [UxmlAttribute("label")]
        public string Label
        {
            get => _labelElement?.text ?? "";
            set
            {
                if (_labelElement != null)
                    _labelElement.text = value;
            }
        }

        [UxmlAttribute("selected-index")]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value >= 0 && value < _choices.Count)
                {
                    _selectedIndex = value;
                    UpdateButtonText();
                    UpdateSelectedItem();
                }
            }
        }

        public string Value
        {
            get =>
                _choices.Count > _selectedIndex && _selectedIndex >= 0
                    ? _choices[_selectedIndex]
                    : "";
            set
            {
                int index = _choices.IndexOf(value);
                if (index >= 0)
                    SelectedIndex = index;
            }
        }

        public List<string> Choices
        {
            get => new List<string>(_choices);
            set => SetChoices(value);
        }

        public CustomDropdown()
        {
            AddToClassList("custom-dropdown");

            _labelElement = new Label { name = "dropdown-label" };
            _labelElement.AddToClassList("custom-dropdown__label");
            Add(_labelElement);

            var buttonContainer = new VisualElement { name = "button-container" };
            buttonContainer.AddToClassList("custom-dropdown__button-container");

            _button = new Button { name = "dropdown-button" };
            _button.AddToClassList("custom-dropdown__button");
            _button.clicked += ToggleMenu;
            buttonContainer.Add(_button);

            _arrowIcon = new Label { name = "dropdown-arrow", text = "▼" };
            _arrowIcon.AddToClassList("custom-dropdown__arrow");
            buttonContainer.Add(_arrowIcon);

            Add(buttonContainer);

            _menu = new VisualElement { name = "dropdown-menu" };
            _menu.AddToClassList("custom-dropdown__menu");
            _menu.style.display = DisplayStyle.None;

            _list = new ScrollView { name = "dropdown-list" };
            _list.AddToClassList("custom-dropdown__list");
            _menu.Add(_list);

            Add(_menu);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            _button.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            RegisterCallback<WheelEvent>(evt =>
            {
                if (_isOpen)
                    CloseMenu();
            });
        }

        public void SetChoices(List<string> choices)
        {
            _choices = new List<string>(choices);
            if (_selectedIndex >= _choices.Count)
                _selectedIndex = _choices.Count > 0 ? 0 : 0;

            RebuildList();
            UpdateButtonText();
        }

        public void SetValueWithoutNotify(string value)
        {
            int index = _choices.IndexOf(value);
            if (index >= 0)
            {
                _selectedIndex = index;
                UpdateButtonText();
                UpdateSelectedItem();
            }
        }

        private void RebuildList()
        {
            _list.Clear();

            for (int i = 0; i < _choices.Count; i++)
            {
                int index = i;
                var item = new Button { text = _choices[i], name = $"item-{i}" };
                item.AddToClassList("custom-dropdown__item");

                if (i == _selectedIndex)
                    item.AddToClassList("custom-dropdown__item--selected");

                item.clicked += () => SelectItem(index);
                _list.Add(item);
            }
        }

        private void SelectItem(int index)
        {
            if (index == _selectedIndex)
            {
                CloseMenu();
                return;
            }

            _selectedIndex = index;
            UpdateButtonText();
            UpdateSelectedItem();
            CloseMenu();

            OnValueChanged?.Invoke(_selectedIndex);

            using (var evt = ChangeEvent<string>.GetPooled(Value, Value))
            {
                evt.target = this;
                SendEvent(evt);
            }
        }

        private void UpdateButtonText()
        {
            _button.text =
                _choices.Count > _selectedIndex && _selectedIndex >= 0
                    ? _choices[_selectedIndex]
                    : "Select...";
        }

        private void UpdateSelectedItem()
        {
            var items = _list.Query<Button>(className: "custom-dropdown__item").ToList();
            for (int i = 0; i < items.Count; i++)
            {
                if (i == _selectedIndex)
                    items[i].AddToClassList("custom-dropdown__item--selected");
                else
                    items[i].RemoveFromClassList("custom-dropdown__item--selected");
            }
        }

        private void ToggleMenu()
        {
            if (_isOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        private void OpenMenu()
        {
            _isOpen = true;
            _menu.style.display = DisplayStyle.Flex;
            _arrowIcon.AddToClassList("custom-dropdown__arrow--open");

            if (panel != null)
            {
                VisualElement rootContainer = this;
                while (rootContainer.parent != null && rootContainer.parent != panel.visualTree)
                {
                    rootContainer = rootContainer.parent;
                }

                rootContainer.Add(_menu);

                var btnRect = _button.worldBound;
                var localPos = rootContainer.WorldToLocal(new Vector2(btnRect.x, btnRect.yMax));

                _menu.style.top = localPos.y;
                _menu.style.left = localPos.x;
                _menu.style.width = btnRect.width;
                _menu.style.right = StyleKeyword.Auto;

                _menu.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            }

            var selected = _list
                .Query<Button>(className: "custom-dropdown__item--selected")
                .First();
            if (selected != null)
            {
                _list.ScrollTo(selected);
            }
        }

        private void CloseMenu()
        {
            _isOpen = false;
            _menu.style.display = DisplayStyle.None;
            _arrowIcon.RemoveFromClassList("custom-dropdown__arrow--open");

            Add(_menu);

            _menu.style.top = StyleKeyword.Null;
            _menu.style.left = StyleKeyword.Null;
            _menu.style.width = StyleKeyword.Null;
            _menu.style.right = StyleKeyword.Null;

            _menu.UnregisterCallback<PointerDownEvent>(e => e.StopPropagation());
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_isOpen && !IsSelfOrDescendant(evt.target as VisualElement))
            {
                CloseMenu();
            }
        }

        private bool IsSelfOrDescendant(VisualElement element)
        {
            while (element != null)
            {
                if (element == this)
                    return true;
                element = element.parent;
            }
            return false;
        }
    }
}
