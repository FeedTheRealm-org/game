using Game.Core.Client.Enum;

namespace Game.Core.Common.RpcMessages;

/// <summary>
/// ServerResponse represents a response from the server, containing the content of the response and its type.
/// </summary>
public struct ServerResponseDTO
{
    public byte[] content;
    public ServerResponseType Type;
}
