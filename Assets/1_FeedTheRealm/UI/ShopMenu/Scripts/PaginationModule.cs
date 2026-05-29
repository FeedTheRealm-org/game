using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.Shop
{
    public class PaginationModule<T>
    {
        private readonly int _itemsPerPage;
        private readonly List<T> _allItems = new List<T>();
        private int _currentPage = 0;
        private Action<T> _renderItem;
        private VisualElement _container;
        private VisualElement _paginationControls;
        private Label _pageLabel;

        public PaginationModule(int itemsPerPage, VisualElement container, Action<T> renderItem)
        {
            _itemsPerPage = itemsPerPage;
            _container = container;
            _renderItem = renderItem;

            _paginationControls = new VisualElement();
            _paginationControls.AddToClassList("shop-pagination-controls");
            _paginationControls.style.flexDirection = FlexDirection.Row;
            _paginationControls.style.justifyContent = Justify.Center;
            _paginationControls.style.marginTop = 10;
            _paginationControls.style.marginBottom = 10;
            _paginationControls.style.alignItems = Align.Center;

            var prevButton = new Button(PreviousPage) { text = "<" };
            prevButton.AddToClassList("shop-pagination-button");

            _pageLabel = new Label();
            _pageLabel.AddToClassList("shop-pagination-label");
            _pageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _pageLabel.style.width = 60;
            _pageLabel.style.color = new StyleColor(new Color32(42, 210, 188, 255));
            _pageLabel.style.fontSize = 18;
            _pageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var nextButton = new Button(NextPage) { text = ">" };
            nextButton.AddToClassList("shop-pagination-button");

            Action<Button> applyStyle = (btn) =>
            {
                btn.style.width = 40;
                btn.style.height = 40;
                btn.style.fontSize = 20;
                btn.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                btn.style.borderTopWidth = 0;
                btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0;
                btn.style.borderRightWidth = 0;
                btn.style.color = new StyleColor(new Color32(42, 210, 188, 255));
                btn.style.unityFontStyleAndWeight = FontStyle.Bold;

                btn.RegisterCallback<PointerEnterEvent>(e =>
                {
                    btn.style.backgroundColor = new StyleColor(new Color32(42, 210, 188, 77));
                });
                btn.RegisterCallback<PointerLeaveEvent>(e =>
                {
                    btn.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                });
            };

            applyStyle(prevButton);
            applyStyle(nextButton);

            _paginationControls.Add(prevButton);
            _paginationControls.Add(_pageLabel);
            _paginationControls.Add(nextButton);
        }

        public void SetItems(IEnumerable<T> items)
        {
            _allItems.Clear();
            _allItems.AddRange(items);
            _currentPage = 0;
            RenderPage();
        }

        private void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                RenderPage();
            }
        }

        private void NextPage()
        {
            if ((_currentPage + 1) * _itemsPerPage < _allItems.Count)
            {
                _currentPage++;
                RenderPage();
            }
        }

        private void RenderPage()
        {
            _container.Clear();

            int startIndex = _currentPage * _itemsPerPage;
            int endIndex = Math.Min(startIndex + _itemsPerPage, _allItems.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                _renderItem(_allItems[i]);
            }

            int totalPages = Math.Max(1, (_allItems.Count + _itemsPerPage - 1) / _itemsPerPage);
            _pageLabel.text = $"{_currentPage + 1} / {totalPages}";

            if (totalPages > 1)
            {
                _container.Add(_paginationControls);
            }
        }
    }
}
