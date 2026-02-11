using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Commands;

// <summary>
// Factory class to create server command instances from DTOs.
// </summary>
public static class CommandsFactory
{
    public static BaseServerCommand FromActionCommandDTO(ActionCommandDTO actionCommandDTO)
    {
        switch (actionCommandDTO.Type)
        {
            case ActionType.Move:
                return new MoveCommand(actionCommandDTO.Direction);
            case ActionType.Dash:
                return new DashCommand(actionCommandDTO.Direction);
            case ActionType.Use:
                return new UseCommand(actionCommandDTO.Direction);
            case ActionType.Interact:
                return new InteractCommand(actionCommandDTO.Direction);
            default:
                throw new System.ArgumentException(
                    $"Unsupported action type: {actionCommandDTO.Type}"
                );
        }
    }

    public static BaseServerCommand FromActionCommandDTO(
        TransactionCommandDTO transactionCommandDTO
    )
    {
        switch (transactionCommandDTO.Type)
        {
            case TransactionType.Equip:
                return new EquipCommand(transactionCommandDTO.Id);
            case TransactionType.Drop:
                return new DropCommand(transactionCommandDTO.Id);
            case TransactionType.Purchase:
                return new PurchaseCommand(transactionCommandDTO.Id);
            case TransactionType.AcceptQuest:
                return new AcceptQuestCommand(transactionCommandDTO.Id);
            default:
                throw new System.ArgumentException(
                    $"Unsupported transaction type: {transactionCommandDTO.Type}"
                );
        }
    }
}
