using System;
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
                return new InteractCommand(dto.NetId);
            case ActionType.CancelInteract:
                return new CancelInteractCommand(dto.NetId);
            case ActionType.DialogNext:
                return new DialogNextCommand(dto.NetId);
            default:
                throw new ArgumentException($"Unsupported action type: {dto.Type}");
        }
    }

    public static BaseServerCommand FromTransactionCommandDTO(TransactionCommandDTO dto)
    {
        switch (dto.Type)
        {
            case TransactionType.EquipItem:
                try
                {
                    EquipItemCommandContent content = EquipItemCommandContent.Parser.ParseFrom(
                        dto.content
                    );
                    return new EquipItemCommand(dto.NetId, dto.Id, content);
                }
                catch
                {
                    EquipItemCommandContent defaultContent = new EquipItemCommandContent
                    {
                        Position = -1,
                    };
                    return new EquipItemCommand(dto.NetId, dto.Id, defaultContent);
                }
            case TransactionType.DropItem:
                try
                {
                    DropItemCommandContent content = DropItemCommandContent.Parser.ParseFrom(
                        dto.content
                    );
                    return new DropItemCommand(dto.NetId, dto.Id, content);
                }
                catch
                {
                    DropItemCommandContent defaultContent = new DropItemCommandContent
                    {
                        Type = StorageType.Null,
                        Position = -1,
                    };
                    return new DropItemCommand(dto.NetId, dto.Id, defaultContent);
                }
            case TransactionType.Purchase:
                try
                {
                    PurchaseCommandContent content = PurchaseCommandContent.Parser.ParseFrom(
                        dto.content
                    );
                    return new PurchaseCommand(dto.NetId, dto.Id, content);
                }
                catch
                {
                    PurchaseCommandContent defaultContent = new PurchaseCommandContent
                    {
                        ProductId = string.Empty,
                        Amount = 0,
                    };
                    return new PurchaseCommand(dto.NetId, dto.Id, defaultContent);
                }
            case TransactionType.AcceptQuest:
                return new AcceptQuestCommand(dto.NetId, dto.Id);
            case TransactionType.RejectQuest:
                return new RejectQuestCommand(dto.NetId);
            case TransactionType.MoveItem:
                try
                {
                    MoveItemCommandContent content = MoveItemCommandContent.Parser.ParseFrom(
                        dto.content
                    );
                    return new MoveItemCommand(dto.NetId, dto.Id, content);
                }
                catch
                {
                    MoveItemCommandContent defaultContent = new MoveItemCommandContent
                    {
                        SourceType = StorageType.Null,
                        SourcePosition = -1,
                        TargetType = StorageType.Null,
                        TargetPosition = -1,
                    };
                    return new MoveItemCommand(dto.NetId, dto.Id, defaultContent);
                }
            default:
                throw new ArgumentException($"Unsupported transaction type: {dto.Type}");
        }
    }
}
