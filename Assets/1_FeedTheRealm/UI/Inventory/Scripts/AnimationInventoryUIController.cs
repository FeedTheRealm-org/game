using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.Inventory
{
    public class AnimationInventoryUIController : MonoBehaviour
    {
        [SerializeField]
        private const float AnimationDuration = 0.3f;

        [SerializeField]
        private float panelWidth = 500f;
        private IVisualElementScheduledItem animationSchedule;
        private VisualElement inventory;
        private VisualElement panel;

        public void Initialize(VisualElement root)
        {
            inventory = root.Q("Inventory");
            panel = inventory?.Q("Panel");
        }

        public void Toggle()
        {
            if (panel == null)
                return;

            bool show = inventory.resolvedStyle.display == DisplayStyle.None;

            if (show)
            {
                inventory.style.display = DisplayStyle.Flex;
                AnimatePanel(startX: panelWidth, endX: 0f, onComplete: null);
            }
            else
            {
                AnimatePanel(
                    startX: 0f,
                    endX: panelWidth,
                    onComplete: () =>
                    {
                        inventory.style.display = DisplayStyle.None;
                        panel.style.translate = new Translate(0, 0);
                    }
                );
            }
        }

        public bool IsVisible => inventory?.resolvedStyle.display != DisplayStyle.None;

        private void AnimatePanel(float startX, float endX, Action onComplete)
        {
            animationSchedule?.Pause();

            panel.style.translate = new Translate(startX, 0);

            float elapsed = 0f;
            float lastTime = Time.realtimeSinceStartup;

            animationSchedule = panel
                .schedule.Execute(() =>
                {
                    float now = Time.realtimeSinceStartup;
                    elapsed += now - lastTime;
                    lastTime = now;

                    float t = Mathf.Clamp01(elapsed / AnimationDuration);
                    float eased = EaseInOut(t);
                    float currentX = Mathf.Lerp(startX, endX, eased);

                    panel.style.translate = new Translate(currentX, 0);

                    if (t >= 1f)
                    {
                        animationSchedule.Pause();
                        onComplete?.Invoke();
                    }
                })
                .Every(16);
        }

        private float EaseInOut(float t) => t * t * (3f - 2f * t);
    }
}
