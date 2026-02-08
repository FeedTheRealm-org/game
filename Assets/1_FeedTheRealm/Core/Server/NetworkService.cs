using Game.Core.Events;
using Game.Core.RpcMessages;

/// <summary>
/// NetworkService is responsible for receiving commands from NetworkAdapters, and sending events to them.
/// It acts as a bridge between the GameLoop and the NetworkAdapters and **RUNS IN MAIN THREAD**.
/// </summary>
public class NetworkService
{
    private readonly WorldMonitor worldMonitor;
    private ReceivedActionCommandEvent receivedActionCommandEvent;
    private ReceivedTransactionCommandEvent receivedTransactionCommandEvent;

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
        // TODO: Receive packets from NetworkAdapter build Commands and
        // send them to GameLoop via CommandQueue
    }

    private void OnReceivedTransactionCommand(TransactionCommandDTO transactionCommand)
    {
        // TODO: Receive packets from NetworkAdapter build Commands and
        // send them to GameLoop via CommandQueue
    }

    // <summary>
    /// FlushEventsToClients empties the events queue and sends
    /// them to the correspoinding NetworkAdapters.
    /// </summary>
    public void FlushEventsToClients()
    {
        // TODO: Collect packets from GameLoop via NetworkQueue and
        // send them to correspoinding NetworkAdapters
    }
}
