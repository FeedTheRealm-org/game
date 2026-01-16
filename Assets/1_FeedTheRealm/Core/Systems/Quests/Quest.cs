namespace Game.Core.Quests
{
    public abstract class Quest : IQuest
    {
        public abstract void Start();

        public abstract void Dispose();
    }
}
