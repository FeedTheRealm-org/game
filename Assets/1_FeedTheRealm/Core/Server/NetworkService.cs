using System.Diagnostics;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Entities;

/// <summary>
/// NetworkService is responsible for receiving commands from NetworkAdapters, and sending events to them.
/// It acts as a bridge between the GameLoop and the NetworkAdapters and **RUNS IN MAIN THREAD**.
/// </summary>
public class NetworkService
{
    private readonly WorldMonitor worldMonitor;

    private ReceivedActionCommandEvent receivedActionCommandEvent;
    private ReceivedTransactionCommandEvent receivedTransactionCommandEvent;

    private readonly long maxEventTimePerTick = Stopwatch.Frequency / 1000 * 3; // 3ms
    private readonly long maxEventsPerTick = 100;

    public NetworkService(
        WorldMonitor worldMonitor,
        ReceivedActionCommandEvent receivedActionCommandEvent,
        ReceivedTransactionCommandEvent receivedTransactionCommandEvent
    )
    {
        this.worldMonitor = worldMonitor;
        this.receivedActionCommandEvent = receivedActionCommandEvent;
        this.receivedTransactionCommandEvent = receivedTransactionCommandEvent;

        this.receivedActionCommandEvent.OnRaised += OnReceivedActionCommand;
        this.receivedTransactionCommandEvent.OnRaised += OnReceivedTransactionCommand;
    }

    private void OnReceivedActionCommand(ActionCommandDTO actionCommand)
    {
        var cmd = CommandsFactory.FromActionCommandDTO(actionCommand);
        worldMonitor.Commands.Enqueue(cmd);
    }

    private void OnReceivedTransactionCommand(TransactionCommandDTO transactionCommand)
    {
        UnityEngine.Debug.Log($"Received Transaction Command: {transactionCommand.Type}");
        var cmd = CommandsFactory.FromTransactionCommandDTO(transactionCommand);
        worldMonitor.Commands.Enqueue(cmd);
    }

    // <summary>
    /// FlushEventsToClients empties the events queue and sends
    /// them to the correspoinding NetworkAdapters.
    /// </summary>
    public void FlushEventsToClients()
    {
        var start = Stopwatch.GetTimestamp();
        int processedThisTick = 0;

        while (
            processedThisTick < maxEventsPerTick
            && (Stopwatch.GetTimestamp() - start) < maxEventTimePerTick
            && worldMonitor.Events.TryDequeue(out var serverEvent)
        )
        {
            if (worldMonitor.Entities.TryGet(serverEvent.NetId, out ServerEntity entity))
            {
                entity.NetworkAdapter.DispatchEvent(serverEvent.ToDTO());
                processedThisTick++;
            }
        }
    }
}
