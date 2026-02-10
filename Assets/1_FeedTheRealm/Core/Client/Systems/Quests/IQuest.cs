using System;

namespace FTR.Core.Client.Quests
{
    public interface IQuest : IDisposable
    {
        void Start();
    }
}
