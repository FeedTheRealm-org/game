using System;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Input;

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
    Confirmation,
}

public class MenuManager : IDisposable
{
    private CursorManager cursorManager;
    private Logging.Logger logger;

    private BackEvent backEvent;

    private readonly Dictionary<MenuType, bool> menuStatus = new Dictionary<MenuType, bool>
    {
        { MenuType.Settings, false },
        { MenuType.Inventory, false },
        { MenuType.Shop, false },
        { MenuType.Quest, false },
        { MenuType.Chat, false },
        { MenuType.Portal, false },
        { MenuType.Quests, false },
        { MenuType.Confirmation, false },
    };

    private readonly Dictionary<MenuType, (Action, Action)> menuActionCallbacks = new();

    private int openMenuCount = 0;
    private bool isMainMenu = false;

    public event Action<MenuType> OnMenuOpened;
    public event Action<MenuType, bool> OnMenuStatusChanged;

    public MenuManager(CursorManager cursorManager, BackEvent backEvent, Logging.Logger logger)
    {
        this.cursorManager = cursorManager;
        this.backEvent = backEvent;
        this.logger = logger;
        backEvent.OnRaised += HandleBackEvent;
    }

    public void Dispose()
    {
        backEvent.OnRaised -= HandleBackEvent;
    }

    public void ToggleMenu(MenuType menuType, bool isOpen)
    {
        if (
            menuStatus[menuType] == isOpen
            || (isOpen && !CanOpenMenu(menuType))
            || (!isOpen && !CanCloseMenu(menuType))
        )
            return;

        menuStatus[menuType] = isOpen;

        var prevCount = openMenuCount;
        openMenuCount += isOpen ? 1 : -1;

        if (prevCount == 0 && openMenuCount > 0)
        {
            showCursor(false);
            OnMenuOpened?.Invoke(menuType);
        }
        else if (prevCount > 0 && openMenuCount == 0)
            showCursor(true);
        OnMenuStatusChanged?.Invoke(menuType, isOpen);
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
            case MenuType.Confirmation:
                return true;
            default:
                return false;
        }
    }

    public bool CanCloseMenu(MenuType menuType)
    {
        if (!menuStatus[menuType])
            return false;

        if (menuStatus[MenuType.Confirmation])
            return menuType == MenuType.Confirmation;

        return true;
    }

    public bool AreAnyMenusOpen()
    {
        return openMenuCount > 0;
    }

    public void RegisterMenuCallbacks(MenuType menuType, Action onOpen, Action onClose)
    {
        logger.Log(
            $"[MenuManager] Registering callbacks for {menuType}: onOpen={onOpen != null}, onClose={onClose != null}"
        );
        menuActionCallbacks[menuType] = (onOpen, onClose);
    }

    private void HandleBackEvent()
    {
        if (menuStatus[MenuType.Confirmation] && CanCloseMenu(MenuType.Confirmation))
        {
            menuActionCallbacks.TryGetValue(MenuType.Confirmation, out var cb);
            if (cb.Item2 != null)
                cb.Item2?.Invoke();
            return;
        }

        if (menuStatus[MenuType.Settings] && CanCloseMenu(MenuType.Settings))
        {
            logger.Log("[MenuManager] BackEvent: Closing Settings menu.");
            menuActionCallbacks.TryGetValue(MenuType.Settings, out var cb);
            if (cb.Item2 != null)
                cb.Item2?.Invoke();
        }
        else if (!menuStatus[MenuType.Settings] && CanOpenMenu(MenuType.Settings) && !isMainMenu)
        {
            logger.Log("[MenuManager] BackEvent: Opening Settings menu.");
            menuActionCallbacks.TryGetValue(MenuType.Settings, out var cb);
            if (cb.Item1 != null)
                cb.Item1?.Invoke();
        }

        foreach (var menu in menuStatus)
        {
            logger.Log($"[MenuManager] BackEvent: Checking menu status {menu.Key}: {menu.Value}");
            if (menu.Value && CanCloseMenu(menu.Key) && menu.Key != MenuType.Settings)
            {
                menuActionCallbacks.TryGetValue(menu.Key, out var cb);
                if (cb.Item2 != null)
                    cb.Item2?.Invoke();
                break;
            }
        }
    }

    private void showCursor(bool show)
    {
        if (isMainMenu)
            return;

        cursorManager.ToggleCursorBlock(show);
    }

    public void SetIsMainMenu(bool isMainMenu)
    {
        this.isMainMenu = isMainMenu;
    }
}
