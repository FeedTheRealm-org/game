using System;
using FTRShared.Runtime.Models;

namespace FeedTheRealm.Core.Interfaces
{
    public interface IDialogBox // TODO(refactor): unify with IChatBox since they should have the same behaviour
    {
        void ShowDialogMessage(MessageData message);
        void ToggleDialog(bool isOpen);
    }
}
