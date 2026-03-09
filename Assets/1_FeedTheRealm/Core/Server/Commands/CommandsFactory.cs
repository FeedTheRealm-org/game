using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Commands;

// <summary>
// Factory class to create server command instances from DTOs.
// </summary>
public static class CommandsFactory
{
    public static BaseServerCommand FromActionCommandDTO(ActionCommandDTO dto)
    {
        switch (dto.Type)
        {
            case ActionType.Move:
                return new MoveCommand(dto.NetId, dto.Direction);
            case ActionType.Dash:
                return new DashCommand(dto.NetId, dto.Direction);
            case ActionType.Use:
                return new UseCommand(dto.NetId, dto.Direction);
            case ActionType.Interact:
                return new InteractCommand(dto.NetId, dto.Direction);
            default:
                throw new System.ArgumentException($"Unsupported action type: {dto.Type}");
        }
    }

    public static BaseServerCommand FromTransactionCommandDTO(TransactionCommandDTO dto)
    {
        switch (dto.Type)
        {
            case TransactionType.Equip:
                return new EquipCommand(dto.NetId, dto.Id);
            case TransactionType.Drop:
                return new DropCommand(dto.NetId, dto.Id);
            case TransactionType.Purchase:
                return new PurchaseCommand(dto.NetId, dto.Id);
            case TransactionType.AcceptQuest:
                return new AcceptQuestCommand(dto.NetId, dto.Id);
            case TransactionType.PickUp:
                return new PickUpCommand(dto.NetId, dto.Id);
            default:
                throw new System.ArgumentException($"Unsupported transaction type: {dto.Type}");
        }
    }
}
