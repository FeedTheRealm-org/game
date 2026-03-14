using System;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEditor;

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
                throw new ArgumentException($"Unsupported action type: {dto.Type}");
        }
    }

    public static BaseServerCommand FromTransactionCommandDTO(TransactionCommandDTO dto)
    {
        switch (dto.Type)
        {
            case TransactionType.Equip:
                return new EquipCommand(dto.NetId, dto.Id);
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
                        Position = -1,
                    };
                    return new DropItemCommand(dto.NetId, dto.Id, defaultContent);
                }
            case TransactionType.Purchase:
                return new PurchaseCommand(dto.NetId, dto.Id);
            case TransactionType.AcceptQuest:
                return new AcceptQuestCommand(dto.NetId, dto.Id);
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
                        SourcePosition = -1,
                        TargetPosition = -1,
                    };
                    return new MoveItemCommand(dto.NetId, dto.Id, defaultContent);
                }
            default:
                throw new ArgumentException($"Unsupported transaction type: {dto.Type}");
        }
    }
}
