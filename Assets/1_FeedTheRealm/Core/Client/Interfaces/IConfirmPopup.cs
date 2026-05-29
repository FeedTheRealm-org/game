using System;

namespace FTR.Core.Client.Interfaces
{
    public interface IConfirmPopup
    {
        void Show(
            string question,
            Action onConfirm,
            Action onCancel = null,
            string title = "Confirm Action",
            string confirmText = null,
            string cancelText = null
        );

        void Hide();
    }
}
