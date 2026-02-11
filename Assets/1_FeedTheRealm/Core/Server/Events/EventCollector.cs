using System;
using System.Collections.Generic;
using FTR.Core.Common.Utils;

namespace FTR.Core.Server.Events;

public sealed class EventCollector : IEventCollectable
{
    private readonly List<BaseServerEvent> events = new();

    public void Collect(BaseServerEvent serverEvent)
    {
        events.Add(serverEvent);
    }

    public void Clear()
    {
        events.Clear();
    }

    public void ForEach(Action<BaseServerEvent> action)
    {
        foreach (var serverEvent in events)
        {
            action(serverEvent);
        }
    }
}
