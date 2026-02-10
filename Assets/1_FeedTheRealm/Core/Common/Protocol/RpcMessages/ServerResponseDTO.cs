using FTR.Core.Client.Enum;

namespace FTR.Core.Common.Protocol.RpcMessages;

/// <summary>
/// ServerResponse represents a response from the server, containing the content of the response and its type.
/// </summary>
public struct ServerResponseDTO
{
    public byte[] content;
    public ServerResponseType Type;
}
