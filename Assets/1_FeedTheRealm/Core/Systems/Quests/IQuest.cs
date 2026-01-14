using System;
using Game.Core.Events;

namespace Game.Core.Quests
{
    public interface IQuest : IDisposable
    {
        void Start();
    }
}
