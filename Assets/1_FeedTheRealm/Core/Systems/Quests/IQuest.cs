using System;

namespace Game.Core.Quests
{
    public interface IQuest : IDisposable
    {
        void Start();
    }
}
