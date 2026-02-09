using System;

namespace Game.Core.Client.Quests
{
    public interface IQuest : IDisposable
    {
        void Start();
    }
}
