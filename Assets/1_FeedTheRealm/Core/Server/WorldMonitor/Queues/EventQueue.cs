using System.Collections.Generic;
using FTR.Core.Server.Events;

public sealed class EventQueue
{
    private readonly Queue<ServerEvent> queue = new();

    public void Enqueue(ServerEvent evt)
    {
        queue.Enqueue(evt);
    }

    public bool TryDequeue(out ServerEvent evt)
    {
        return queue.TryDequeue(out evt);
    }
}
