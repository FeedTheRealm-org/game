using FTR.Core.Client.EventChannels.Chat;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;
using UnityEngine;
using VContainer;

public class ChatController : MonoBehaviour
{
    [Inject]
    private ChatMessageRequestEvent chatMessageRequestEvent;

    private NetworkAdapter networkAdapter;
    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;

        if (chatMessageRequestEvent != null)
            chatMessageRequestEvent.OnRaised += OnChatMessageRequest;
    }

    private void OnDestroy()
    {
        if (chatMessageRequestEvent != null)
            chatMessageRequestEvent.OnRaised -= OnChatMessageRequest;
    }

    private void OnChatMessageRequest(string message)
    {
        if (!isInitialized)
            return;

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.SendMessage,
            Id = string.Empty,
            content = new SendMessageCommandContent { Message = message }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
