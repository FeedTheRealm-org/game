using System;

namespace FTR.Core.Common.Quests
{
    public interface IQuest : IDisposable
    {
        void Start();
    }
}
