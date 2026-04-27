using System.Collections.Generic;
using FTR.Core.Server.Events;

public sealed class EventQueue
{
    private readonly Queue<BaseServerEvent> queue = new();

    public void Enqueue(BaseServerEvent evt)
    {
        queue.Enqueue(evt);
    }

    public bool TryDequeue(out BaseServerEvent evt)
    {
        return queue.TryDequeue(out evt);
    }
}
