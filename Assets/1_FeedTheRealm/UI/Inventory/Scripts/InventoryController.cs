using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour
{
    [Header("UI References")]
    private UIDocument uiDocument;
    private VisualElement root;
    private List<VisualElement> slots = new List<VisualElement>();
    private VisualElement draggedItem;
    private VisualElement draggedItemOriginalSlot;
    private Vector2 dragOffset;
    private PlayerControls playerControls;
    public PlayerInputReader playerInputReader;

    [Header("Loot Settings")]
    [SerializeField] private List<Sprite> lootSprites = new List<Sprite>();
    private int currentLootIndex = 0;
    
    [Header("Slot Sprites")]
    [SerializeField] private Sprite slotNormalSprite;
    [SerializeField] private Sprite slotHoverSprite;

    [Header("Logging")]
    [SerializeField] private Logging.Logger logger;
    
    private Button lootButton;
    private VisualElement dropZone;
    private bool isInventoryOpen = false;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Ocultar el inventario inicialmente
        root.style.display = DisplayStyle.None;

        // Inicializar controles de input
        playerControls = new PlayerControls();
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
                
                // Configurar sprites de hover si están asignados
                if (slotNormalSprite != null && slotHoverSprite != null)
                {
                    slot.RegisterCallback<PointerEnterEvent>(evt => OnSlotHoverEnter(evt, slot));
                    slot.RegisterCallback<PointerLeaveEvent>(evt => OnSlotHoverLeave(evt, slot));
                }
            }
        }

        // Configurar botón de Loot
        lootButton = root.Q<Button>("Loot");
        if (lootButton != null)
        {
            lootButton.clicked += OnLootButtonClicked;
        }

        // Configurar zona de Drop
        dropZone = root.Q<VisualElement>("Drop");
        if (dropZone != null)
        {
            dropZone.RegisterCallback<PointerUpEvent>(OnDropZonePointerUp);
        }

        // Registrar eventos globales para drag
        // PointerMove con TrickleDown para capturar en toda la pantalla
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        
        // PointerUp SIN TrickleDown para que se ejecute DESPUÉS de OnSlotPointerUp
        root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        
        // También registrar en el panel para asegurar cobertura completa
        var panel = root.panel;
        if (panel != null)
        {
            panel.visualTree.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            // PointerUp en el panel también sin TrickleDown
            panel.visualTree.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }
    }

    private void OnSlotPointerDown(PointerDownEvent evt)
    {
        logger.Log($"OnSlotPointerDown - Target: {evt.target}, CurrentTarget: {evt.currentTarget}", this);
        
        // Intentar obtener el slot desde currentTarget (el elemento que registró el callback)
        var slot = evt.currentTarget as VisualElement;
        
        if (slot != null && slot.childCount > 0)
        {
            logger.Log($"Slot encontrado con {slot.childCount} hijo(s)", this);
            draggedItem = slot[0]; // Asumir que el primer hijo es el item
            draggedItemOriginalSlot = slot; // Guardar el slot original
            
            // Obtener posición del item antes de removerlo
            Rect itemWorldBound = draggedItem.worldBound;
            
            // Remover del slot y añadir al root para que esté visible sobre todo
            draggedItem.RemoveFromHierarchy();
            root.Add(draggedItem);
            draggedItem.BringToFront();
            
            // Calcular offset usando la posición mundial del item
            dragOffset = new Vector2(evt.position.x - itemWorldBound.x, evt.position.y - itemWorldBound.y);
            
            // Convertir posición mundial a local del root
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);
            
            // Posicionar inmediatamente en la posición correcta
            draggedItem.style.position = Position.Absolute;
            draggedItem.style.left = pointerInRoot.x - dragOffset.x;
            draggedItem.style.top = pointerInRoot.y - dragOffset.y;

            logger.Log($"Iniciando drag - Item: {draggedItem.name}, Offset: {dragOffset}, ItemWorldPos: ({itemWorldBound.x}, {itemWorldBound.y})", this);
            evt.StopPropagation();
        }
        else
        {
            logger.Log($"No se pudo iniciar drag - Slot null: {slot == null}, ChildCount: {slot?.childCount ?? 0}", this, Logging.LogType.Warning);
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (draggedItem != null)
        {
            // Convertir posición global del puntero a coordenadas locales del root
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);
            
            // Aplicar offset
            draggedItem.style.left = pointerInRoot.x - dragOffset.x;
            draggedItem.style.top = pointerInRoot.y - dragOffset.y;
            draggedItem.style.position = Position.Absolute;
            
            // Prevenir que el evento se propague
            evt.StopPropagation();
            // Debug.Log($"Dragging - PointerInRoot: {pointerInRoot}, ItemPos: ({draggedItem.style.left.value.value}, {draggedItem.style.top.value.value})");
        }
    }

    private void OnSlotPointerUp(PointerUpEvent evt)
    {
        logger.Log($"OnSlotPointerUp - Target: {evt.target}, DraggedItem: {draggedItem != null}", this);
        
        if (draggedItem != null)
        {
            var targetSlot = evt.currentTarget as VisualElement;
            logger.Log($"Target slot: {targetSlot?.name}, Is in slots list: {slots.Contains(targetSlot)}", this);
            
            if (targetSlot != null && slots.Contains(targetSlot))
            {
                // Mover/intercambiar el item al slot destino
                logger.Log($"Moviendo/intercambiando item a slot: {targetSlot.name}", this);
                MoveItemToSlot(draggedItem, targetSlot);
                
                // Limpiar referencias DESPUÉS de mover
                draggedItem = null;
                draggedItemOriginalSlot = null;
                
                // Importante: detener inmediatamente la propagación
                evt.StopImmediatePropagation();
            }
        }
    }

    private void OnGlobalPointerUp(PointerUpEvent evt)
    {
        logger.Log($"OnGlobalPointerUp - DraggedItem: {draggedItem != null}, Position: {evt.position}", this);
        
        // Solo procesar si todavía hay un item siendo arrastrado
        // (si OnSlotPointerUp lo manejó, draggedItem será null)
        if (draggedItem != null)
        {
            // Verificar si se soltó sobre la zona de drop
            bool isOverDrop = IsPointerOverElement(evt.position, dropZone);
            logger.Log($"Is over drop zone: {isOverDrop}, DropZone null: {dropZone == null}", this);
            
            if (isOverDrop)
            {
                // Eliminar el item
                draggedItem.RemoveFromHierarchy();
                logger.Log("Item eliminado en zona de Drop (Global)", this);
            }
            else
            {
                // Si se suelta fuera de un slot, devolver
                logger.Log("Devolviendo item (desde global)", this);
                ReturnItemToOriginalSlot();
            }
            
            draggedItem = null;
            draggedItemOriginalSlot = null;
        }
    }

    private void OnDropZonePointerUp(PointerUpEvent evt)
    {
        if (draggedItem != null)
        {
            // Eliminar el item arrastrado
            draggedItem.RemoveFromHierarchy();
            logger.Log("Item eliminado en zona de Drop", this);
            draggedItem = null;
            draggedItemOriginalSlot = null;
            evt.StopPropagation();
        }
    }

    private void OnSlotHoverEnter(PointerEnterEvent evt, VisualElement slot)
    {
        if (slotHoverSprite != null)
        {
            slot.style.backgroundImage = new StyleBackground(slotHoverSprite);
        }
    }

    private void OnSlotHoverLeave(PointerLeaveEvent evt, VisualElement slot)
    {
        if (slotNormalSprite != null)
        {
            slot.style.backgroundImage = new StyleBackground(slotNormalSprite);
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
        // Si el slot destino tiene un item Y no es el slot original, intercambiar
        if (targetSlot.childCount > 0 && targetSlot != draggedItemOriginalSlot)
        {
            logger.Log("Slot ocupado, intercambiando items", this);
            var targetItem = targetSlot[0];
            
            // Remover ambos items
            item.RemoveFromHierarchy();
            targetItem.RemoveFromHierarchy();
            
            // Intercambiar posiciones
            targetSlot.Add(item);
            if (draggedItemOriginalSlot != null)
            {
                draggedItemOriginalSlot.Add(targetItem);
            }
            
            // Resetear estilos de ambos items
            item.style.position = Position.Relative;
            item.style.left = 0;
            item.style.top = 0;
            
            targetItem.style.position = Position.Relative;
            targetItem.style.left = 0;
            targetItem.style.top = 0;
        }
        else
        {
            // Slot vacío o es el mismo slot original, simplemente mover
            item.RemoveFromHierarchy();
            targetSlot.Add(item);
            
            // Resetear posición
            item.style.position = Position.Relative;
            item.style.left = 0;
            item.style.top = 0;
        }
    }

    private void ReturnItemToOriginalSlot()
    {
        // Intentar devolver al slot original si lo tenemos guardado
        if (draggedItemOriginalSlot != null)
        {
            draggedItemOriginalSlot.Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
            return;
        }
        
        // Si no, buscar un slot vacío
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
        
        // Si no hay slots vacíos, añadir al primero
        if (slots.Count > 0)
        {
            slots[0].Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
        }
    }

    private bool IsPointerOverElement(Vector2 pointerPosition, VisualElement element)
    {
        if (element == null) return false;
        
        var rect = element.worldBound;
        return rect.Contains(pointerPosition);
    }

    private void OnLootButtonClicked()
    {
        if (lootSprites == null || lootSprites.Count == 0)
        {
            logger.Log("No hay sprites asignados en la lista de loot", this, Logging.LogType.Warning);
            return;
        }

        // Obtener el sprite actual de la lista
        Sprite spriteToAdd = lootSprites[currentLootIndex];
        
        // Avanzar al siguiente índice (con wrap-around)
        currentLootIndex = (currentLootIndex + 1) % lootSprites.Count;
        
        AddItem(spriteToAdd);
        logger.Log($"Looteado sprite {currentLootIndex}/{lootSprites.Count}", this);
    }

    // Método para añadir un item al inventario
    public void AddItem(Sprite itemSprite)
    {
        if (itemSprite == null)
        {
            logger.Log("No se puede añadir un item sin sprite", this, Logging.LogType.Warning);
            return;
        }

        // Buscar el primer slot vacío
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                CreateItemElement(itemSprite, slot);
                logger.Log($"Item añadido al inventario", this);
                return;
            }
        }

        logger.Log("Inventario lleno, no se puede añadir más items", this);
    }

    private void CreateItemElement(Sprite itemSprite, VisualElement parentSlot)
    {
        var itemElement = new VisualElement();
        itemElement.name = "InventoryItem";
        itemElement.style.backgroundImage = new StyleBackground(itemSprite);
        itemElement.style.width = 45;
        itemElement.style.height = 45;
        itemElement.style.position = Position.Relative;
        itemElement.AddToClassList("inventory-item");
        
        // Importante: permitir que los eventos pasen al slot padre
        itemElement.pickingMode = PickingMode.Ignore;
        
        parentSlot.Add(itemElement);
        logger.Log($"Item creado en slot: {parentSlot.name}", this);
    }

    public bool IsInventoryFull()
    {
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                return false;
            }
        }
        return true;
    }

    public int GetEmptySlotCount()
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                count++;
            }
        }
        return count;
    }

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (isInventoryOpen)
        {
            // Abrir inventario
            root.style.display = DisplayStyle.Flex;
            ShowCursor();
            logger.Log("Inventario abierto - Cursor visible", this);
        }
        else
        {
            // Cerrar inventario
            root.style.display = DisplayStyle.None;
            HideCursor();
            logger.Log("Inventario cerrado - Cursor oculto", this);
        }
    }

    private void ShowCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void HideCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
}