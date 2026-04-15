using FeedTheRealm.Core.Interfaces;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using VContainer;

/// <summary>
/// Transforms chat messages received from the server into UI updates on the client.
/// </summary>
public class ChatView : MonoBehaviour
{
    private NetworkEventRouter eventRouter;
    private uint recvId;
    private IChatBox chatBox;

    public void Initialize(NetworkEventRouter eventRouter, uint recvId, IChatBox chatBox)
    {
        this.eventRouter = eventRouter;
        this.recvId = recvId;
        this.chatBox = chatBox;
        eventRouter.OnChatMessageBroadcastEvent += HandleChatMessageBroadcastEvent;
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnChatMessageBroadcastEvent -= HandleChatMessageBroadcastEvent;
    }

    private void HandleChatMessageBroadcastEvent(ChatMessageBroadcastEventContent content)
    {
        Debug.Log(
            $"ChatView received ChatMessageBroadcastEvent: {content.SenderId}:{content.Message} for netId: {recvId}"
        );
        if (content.SenderId != recvId)
            return;
        chatBox.ShowChatMessage(content.Message);
    }
}
