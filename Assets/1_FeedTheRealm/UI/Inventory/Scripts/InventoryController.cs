using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private List<VisualElement> slots = new List<VisualElement>();
    private VisualElement draggedItem;
    private Vector2 dragOffset;
    private PlayerControls playerControls;
    public PlayerInputReader playerInputReader;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Ocultar el inventario inicialmente
        root.style.display = DisplayStyle.None;

        // Inicializar controles de input
        playerControls = new PlayerControls();
        // IMPORTANTE: Añade una acción "Inventory" (tipo Button) al Input Action Asset en el mapa "Player", asignada a <Keyboard>/i
        // Luego descomenta la línea siguiente y regenera el código del asset.
        playerControls.Player.Inventory.performed += ctx => ToggleInventory();
        playerControls.Player.Enable();

        // Suscribirse al evento del PlayerInputReader si está asignado
        if (playerInputReader != null)
        {
            playerInputReader.InventoryEvent += ToggleInventory;
        }

        // Encontrar todos los slots
        for (int i = 1; i <= 12; i++)
        {
            var slot = root.Q<VisualElement>($"Slot{i}");
            if (slot != null)
            {
                slots.Add(slot);
                slot.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
                slot.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
            }
        }

        // Registrar eventos globales para drag
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
    }

    private void OnSlotPointerDown(PointerDownEvent evt)
    {
        var slot = evt.target as VisualElement;
        if (slot != null && slot.childCount > 0)
        {
            draggedItem = slot[0]; // Asumir que el primer hijo es el item
            dragOffset = new Vector2(evt.localPosition.x, evt.localPosition.y) - draggedItem.layout.position;
            draggedItem.BringToFront();
            evt.StopPropagation();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (draggedItem != null)
        {
            Vector2 newPosition = new Vector2(evt.localPosition.x, evt.localPosition.y) - dragOffset;
            draggedItem.style.left = newPosition.x;
            draggedItem.style.top = newPosition.y;
            draggedItem.style.position = Position.Absolute;
        }
    }

    private void OnSlotPointerUp(PointerUpEvent evt)
    {
        if (draggedItem != null)
        {
            var targetSlot = evt.target as VisualElement;
            if (targetSlot != null && slots.Contains(targetSlot))
            {
                // Mover el item al slot destino
                MoveItemToSlot(draggedItem, targetSlot);
            }
            else
            {
                // Devolver al slot original
                ReturnItemToOriginalSlot();
            }
            draggedItem = null;
        }
    }

    private void OnGlobalPointerUp(PointerUpEvent evt)
    {
        if (draggedItem != null)
        {
            // Si se suelta fuera de un slot, devolver
            ReturnItemToOriginalSlot();
            draggedItem = null;
        }
    }

    void OnDestroy()
    {
        playerControls.Player.Disable();
        playerControls.Dispose();

        if (playerInputReader != null)
        {
            playerInputReader.InventoryEvent -= ToggleInventory;
        }
    }

    private void MoveItemToSlot(VisualElement item, VisualElement targetSlot)
    {
        // Remover de slot actual
        item.RemoveFromHierarchy();

        // Limpiar el slot destino si tiene algo
        if (targetSlot.childCount > 0)
        {
            targetSlot[0].RemoveFromHierarchy();
        }

        // Añadir al slot destino
        targetSlot.Add(item);

        // Resetear posición
        item.style.position = Position.Relative;
        item.style.left = 0;
        item.style.top = 0;
    }

    private void ReturnItemToOriginalSlot()
    {
        // Por simplicidad, asumir que vuelve al primer slot vacío o algo. 
        // En una implementación completa, trackear el slot original.
        // Por ahora, añadir a un slot vacío si existe.
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                slot.Add(draggedItem);
                draggedItem.style.position = Position.Relative;
                draggedItem.style.left = 0;
                draggedItem.style.top = 0;
                return;
            }
        }
        // Si no hay slots vacíos, destruir o algo. Por ahora, añadir al primero.
        if (slots.Count > 0)
        {
            slots[0].Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
        }
    }

    // Método para añadir un item al inventario (para testing)
    public void AddItem(Sprite itemSprite)
    {
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                var itemElement = new VisualElement();
                itemElement.style.backgroundImage = new StyleBackground(itemSprite);
                itemElement.style.width = 40;
                itemElement.style.height = 40;
                itemElement.style.position = Position.Relative;
                slot.Add(itemElement);
                return;
            }
        }
    }

    private void ToggleInventory()
    {
        if (root.style.display == DisplayStyle.Flex)
        {
            root.style.display = DisplayStyle.None;
        }
        else
        {
            root.style.display = DisplayStyle.Flex;
        }
    }
}