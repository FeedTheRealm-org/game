using System;
using System.Collections.Generic;
using UnityEngine;

namespace FTR.Core.Client.Managers;

public enum MenuType
{
    Settings,
    Inventory,
    Shop,
    Quest,
    Chat,
    Portal,
    Quests,
}

public class MenuManager
{
    private CursorManager cursorManager;

    private readonly Dictionary<MenuType, bool> menuStatus = new Dictionary<MenuType, bool>
    {
        { MenuType.Settings, false },
        { MenuType.Inventory, false },
        { MenuType.Shop, false },
        { MenuType.Quest, false },
        { MenuType.Chat, false },
        { MenuType.Portal, false },
        { MenuType.Quests, false },
    };
    private int openMenuCount = 0;

    public event Action OnMenuOpened;

    public MenuManager(CursorManager cursorManager)
    {
        this.cursorManager = cursorManager;
    }

    public void ToggleMenu(MenuType menuType, bool isOpen)
    {
        if (menuStatus[menuType] == isOpen || (isOpen && !CanOpenMenu(menuType)))
            return;

        menuStatus[menuType] = isOpen;

        var prevCount = openMenuCount;
        openMenuCount += isOpen ? 1 : -1;

        if (prevCount == 0 && openMenuCount > 0)
        {
            cursorManager.ToggleCursorBlock(false);
            OnMenuOpened?.Invoke();
        }
        else if (prevCount > 0 && openMenuCount == 0)
            cursorManager.ToggleCursorBlock(true);
    }

    public bool CanOpenMenu(MenuType menuType)
    {
        switch (menuType)
        {
            case MenuType.Settings:
                return openMenuCount == 0;
            case MenuType.Inventory:
                return openMenuCount == 0 || (openMenuCount == 1 && menuStatus[MenuType.Shop]);
            case MenuType.Shop:
                return openMenuCount == 0 || (openMenuCount == 1 && menuStatus[MenuType.Inventory]);
            case MenuType.Quest:
                return openMenuCount == 0;
            case MenuType.Chat:
                return openMenuCount == 0;
            case MenuType.Portal:
                return openMenuCount == 0;
            case MenuType.Quests:
                return openMenuCount == 0;
            default:
                return false;
        }
    }

    public bool AreAnyMenusOpen()
    {
        return openMenuCount > 0;
    }
}
