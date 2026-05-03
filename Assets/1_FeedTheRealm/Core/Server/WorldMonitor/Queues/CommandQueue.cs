using System.Collections.Generic;
using FTR.Core.Server.Commands;

public sealed class CommandQueue
{
    private readonly Queue<BaseServerCommand> queue = new();

    public int Count => queue.Count;

    public void Enqueue(BaseServerCommand cmd)
    {
        queue.Enqueue(cmd);
    }

    public bool TryDequeue(out BaseServerCommand cmd)
    {
        return queue.TryDequeue(out cmd);
    }
}
