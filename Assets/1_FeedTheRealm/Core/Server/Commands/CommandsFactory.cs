using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Commands;

// <summary>
// Factory class to create server command instances from DTOs.
// </summary>
public static class CommandsFactory
{
    public static BaseServerCommand FromActionCommandDTO(
        uint netId,
        ActionCommandDTO actionCommandDTO
    )
    {
        switch (actionCommandDTO.Type)
        {
            case ActionType.Move:
                return new MoveCommand(netId, actionCommandDTO.Direction);
            case ActionType.Dash:
                return new DashCommand(netId, actionCommandDTO.Direction);
            case ActionType.Use:
                return new UseCommand(netId, actionCommandDTO.Direction);
            case ActionType.Interact:
                return new InteractCommand(netId, actionCommandDTO.Direction);
            default:
                throw new System.ArgumentException(
                    $"Unsupported action type: {actionCommandDTO.Type}"
                );
        }
    }

    public static BaseServerCommand FromActionCommandDTO(
        uint netId,
        TransactionCommandDTO transactionCommandDTO
    )
    {
        switch (transactionCommandDTO.Type)
        {
            case TransactionType.Equip:
                return new EquipCommand(netId, transactionCommandDTO.Id);
            case TransactionType.Drop:
                return new DropCommand(netId, transactionCommandDTO.Id);
            case TransactionType.Purchase:
                return new PurchaseCommand(netId, transactionCommandDTO.Id);
            case TransactionType.AcceptQuest:
                return new AcceptQuestCommand(netId, transactionCommandDTO.Id);
            default:
                throw new System.ArgumentException(
                    $"Unsupported transaction type: {transactionCommandDTO.Type}"
                );
        }
    }
}
