using System.Collections.Generic;
using Game.Core.Server.Commands;

public sealed class CommandQueue
{
    private readonly Queue<ServerCommand> queue = new();

    public void Enqueue(ServerCommand cmd)
    {
        queue.Enqueue(cmd);
    }

    public bool TryDequeue(out ServerCommand cmd)
    {
        return queue.TryDequeue(out cmd);
    }
}
