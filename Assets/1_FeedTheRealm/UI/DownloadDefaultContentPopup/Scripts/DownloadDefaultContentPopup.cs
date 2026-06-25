using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Managers;
using FTRShared.Runtime.Core.Cache;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DownloadDefaultContentController : MonoBehaviour
    {
        [Inject]
        private BackEvent backEvent;

        [Inject]
        private Logging.Logger logger;

        [Inject]
        private CacheManager cacheManager;

        [Inject]
        private AssetsService assetsService;

        private VisualElement _overlay;
        private Label _titleLabel;
        private Label _questionLabel;
        private Button _downloadButton;
        private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        private VisualElement _progressBarContainer;
        private Label _downloadingLabel;
        private VisualElement _progressBarFill;

        private bool isDownloading;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            root.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            _overlay = root.Q<VisualElement>("Overlay");
            _titleLabel = root.Q<Label>("DialogTitle");
            _questionLabel = root.Q<Label>("QuestionLabel");
            _downloadButton = root.Q<Button>("ConfirmButton");
            _cancelButton = root.Q<Button>("CancelButton");
            _progressBarContainer = root.Q<VisualElement>("ProgressBarContainer");
            _downloadingLabel = root.Q<Label>("DownloadingLabel");
            _progressBarFill = root.Q<VisualElement>("ProgressBarFill");

            _downloadButton.clicked += OnConfirmClicked;
            _cancelButton.clicked += OnCancelClicked;

            _progressBarContainer.style.display = DisplayStyle.None;
            _downloadingLabel.style.display = DisplayStyle.None;
        }

        private void Start()
        {
            if (backEvent != null)
                backEvent.OnRaised += OnBackPressed;
        }

        private void OnDestroy()
        {
            if (_downloadButton != null)
                _downloadButton.clicked -= OnConfirmClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= OnCancelClicked;
            if (backEvent != null)
                backEvent.OnRaised -= OnBackPressed;
        }

        public void Hide()
        {
            Destroy(gameObject);
        }

        private void OnBackPressed()
        {
            if (this == null)
                return;

            Hide();
        }

        private void OnConfirmClicked()
        {
            _ = OnDownloadButtonClicked();
        }

        private void OnCancelClicked()
        {
            Hide();
        }

        private async Task OnDownloadButtonClicked()
        {
            if (isDownloading)
                return;

            isDownloading = true;
            if (_downloadButton != null)
                _downloadButton.SetEnabled(false);

            if (_cancelButton != null)
                _cancelButton.SetEnabled(false);

            if (_downloadingLabel != null)
                _downloadingLabel.style.display = DisplayStyle.Flex;

            if (_progressBarContainer != null)
                _progressBarContainer.style.display = DisplayStyle.Flex;

            if (_progressBarFill != null)
                _progressBarFill.style.width = new StyleLength(new Length(0, LengthUnit.Percent));

            var cosmeticCategories = await assetsService.GetCategoriesAsync();

            List<SpriteResponse> cosmetics = new();
            foreach (var category in cosmeticCategories.category_list)
            {
                var res = await assetsService.GetSpritesByCategoryAsync(
                    category.category_id,
                    limit: 99999
                );
                cosmetics.AddRange(res.sprites_list);
            }

            int total = cosmetics.Count;
            int completed = 0;
            foreach (var cosmetic in cosmetics)
            {
                try
                {
                    _ = await cacheManager.GetSprite(
                        cosmetic.sprite_url,
                        DateTimeHelper.ParseDateTimeOffset(cosmetic.updated_at)
                    );
                }
                finally
                {
                    completed++;
                }

                if (_downloadingLabel != null)
                    _downloadingLabel.text = $"downloading content... {completed}/{total}";
                if (_progressBarFill != null)
                {
                    float percent = Mathf.Clamp01((float)completed / total) * 100f;
                    _progressBarFill.style.width = new StyleLength(
                        new Length(percent, LengthUnit.Percent)
                    );
                }
            }

            if (_downloadingLabel != null)
                _downloadingLabel.style.display = DisplayStyle.None;

            if (_progressBarContainer != null)
                _progressBarContainer.style.display = DisplayStyle.None;

            if (_downloadButton != null)
                _downloadButton.SetEnabled(true);

            if (_cancelButton != null)
                _cancelButton.SetEnabled(true);

            isDownloading = false;

            await Task.Delay(1000);
            Hide();
        }
    }
}
