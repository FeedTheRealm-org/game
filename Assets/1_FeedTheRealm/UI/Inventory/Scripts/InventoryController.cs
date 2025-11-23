using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour {
    [Header("UI References")]
    private UIDocument uiDocument;
    private VisualElement root;
    private List<VisualElement> slots = new List<VisualElement>();
    private VisualElement draggedItem;
    private VisualElement draggedItemOriginalSlot;
    private Vector2 dragOffset;

    [Header("Items Management")]
    // ItemsManager reference (uses singleton pattern)
    private Items.ItemsManager ItemsManager => Items.ItemsManager.Instance;
    
    [Header("Debug - Loot Testing")]
    [SerializeField] private bool enableDebugLootButton = false;
    [SerializeField] private List<Sprite> debugLootSprites = new List<Sprite>();
    private int currentLootIndex = 0;

    [Header("Slot Sprites")]
    [SerializeField] private Sprite slotNormalSprite;
    [SerializeField] private Sprite slotHoverSprite;

    [Header("Logging")]
    [SerializeField] private Logging.Logger logger;

    private Button lootButton;
    private VisualElement dropZone;

    void Start() {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Encontrar todos los slots
        for (int i = 1; i <= 12; i++) {
            var slot = root.Q<VisualElement>($"Slot{i}");
            if (slot != null) {
                slots.Add(slot);
                slot.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
                slot.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);

                // Configurar sprites de hover si están asignados
                if (slotNormalSprite != null && slotHoverSprite != null) {
                    slot.RegisterCallback<PointerEnterEvent>(evt => OnSlotHoverEnter(evt, slot));
                    slot.RegisterCallback<PointerLeaveEvent>(evt => OnSlotHoverLeave(evt, slot));
                }
            }
        }

        // Configurar botón de Loot (solo para debug)
        lootButton = root.Q<Button>("Loot");
        if (lootButton != null && enableDebugLootButton)
        {
            lootButton.clicked += OnLootButtonClicked;
        }
        else if (lootButton != null)
        {
            // Ocultar el botón si no está en modo debug
            lootButton.style.display = DisplayStyle.None;
        }

        // Configurar zona de Drop
        dropZone = root.Q<VisualElement>("Drop");
        if (dropZone != null) {
            dropZone.RegisterCallback<PointerUpEvent>(OnDropZonePointerUp);
        }

        // Registrar eventos globales para drag
        // PointerMove con TrickleDown para capturar en toda la pantalla
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);

        // PointerUp SIN TrickleDown para que se ejecute DESPUÉS de OnSlotPointerUp
        root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);

        // También registrar en el panel para asegurar cobertura completa
        var panel = root.panel;
        if (panel != null) {
            panel.visualTree.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            // PointerUp en el panel también sin TrickleDown
            panel.visualTree.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }
    }

    private void OnSlotPointerDown(PointerDownEvent evt) {
        logger.Log($"OnSlotPointerDown - Target: {evt.target}, CurrentTarget: {evt.currentTarget}", this);

        // Intentar obtener el slot desde currentTarget (el elemento que registró el callback)
        var slot = evt.currentTarget as VisualElement;

        if (slot != null && slot.childCount > 0) {
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
        } else {
            logger.Log($"No se pudo iniciar drag - Slot null: {slot == null}, ChildCount: {slot?.childCount ?? 0}", this, Logging.LogType.Warning);
        }
    }

    private void OnPointerMove(PointerMoveEvent evt) {
        if (draggedItem != null) {
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

    private void OnSlotPointerUp(PointerUpEvent evt) {
        logger.Log($"OnSlotPointerUp - Target: {evt.target}, DraggedItem: {draggedItem != null}", this);

        if (draggedItem != null) {
            var targetSlot = evt.currentTarget as VisualElement;
            logger.Log($"Target slot: {targetSlot?.name}, Is in slots list: {slots.Contains(targetSlot)}", this);

            if (targetSlot != null && slots.Contains(targetSlot)) {
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

    private void OnGlobalPointerUp(PointerUpEvent evt) {
        logger.Log($"OnGlobalPointerUp - DraggedItem: {draggedItem != null}, Position: {evt.position}", this);

        // Solo procesar si todavía hay un item siendo arrastrado
        // (si OnSlotPointerUp lo manejó, draggedItem será null)
        if (draggedItem != null) {
            // Verificar si se soltó sobre la zona de drop
            bool isOverDrop = IsPointerOverElement(evt.position, dropZone);
            logger.Log($"Is over drop zone: {isOverDrop}, DropZone null: {dropZone == null}", this);

            if (isOverDrop) {
                // Eliminar el item
                draggedItem.RemoveFromHierarchy();
                logger.Log("Item eliminado en zona de Drop (Global)", this);
            } else {
                // Si se suelta fuera de un slot, devolver
                logger.Log("Devolviendo item (desde global)", this);
                ReturnItemToOriginalSlot();
            }

            draggedItem = null;
            draggedItemOriginalSlot = null;
        }
    }

    private void OnDropZonePointerUp(PointerUpEvent evt) {
        if (draggedItem != null) {
            // Eliminar el item arrastrado
            draggedItem.RemoveFromHierarchy();
            logger.Log("Item eliminado en zona de Drop", this);
            draggedItem = null;
            draggedItemOriginalSlot = null;
            evt.StopPropagation();
        }
    }

    private void OnSlotHoverEnter(PointerEnterEvent evt, VisualElement slot) {
        if (slotHoverSprite != null) {
            slot.style.backgroundImage = new StyleBackground(slotHoverSprite);
        }
    }

    private void OnSlotHoverLeave(PointerLeaveEvent evt, VisualElement slot) {
        if (slotNormalSprite != null) {
            slot.style.backgroundImage = new StyleBackground(slotNormalSprite);
        }
    }

    private void MoveItemToSlot(VisualElement item, VisualElement targetSlot) {
        // Si el slot destino tiene un item Y no es el slot original, intercambiar
        if (targetSlot.childCount > 0 && targetSlot != draggedItemOriginalSlot) {
            logger.Log("Slot ocupado, intercambiando items", this);
            var targetItem = targetSlot[0];

            // Remover ambos items
            item.RemoveFromHierarchy();
            targetItem.RemoveFromHierarchy();

            // Intercambiar posiciones
            targetSlot.Add(item);
            if (draggedItemOriginalSlot != null) {
                draggedItemOriginalSlot.Add(targetItem);
            }

            // Resetear estilos de ambos items
            item.style.position = Position.Relative;
            item.style.left = 0;
            item.style.top = 0;

            targetItem.style.position = Position.Relative;
            targetItem.style.left = 0;
            targetItem.style.top = 0;
        } else {
            // Slot vacío o es el mismo slot original, simplemente mover
            item.RemoveFromHierarchy();
            targetSlot.Add(item);

            // Resetear posición
            item.style.position = Position.Relative;
            item.style.left = 0;
            item.style.top = 0;
        }
    }

    private void ReturnItemToOriginalSlot() {
        // Intentar devolver al slot original si lo tenemos guardado
        if (draggedItemOriginalSlot != null) {
            draggedItemOriginalSlot.Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
            return;
        }

        // Si no, buscar un slot vacío
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                slot.Add(draggedItem);
                draggedItem.style.position = Position.Relative;
                draggedItem.style.left = 0;
                draggedItem.style.top = 0;
                return;
            }
        }

        // Si no hay slots vacíos, añadir al primero
        if (slots.Count > 0) {
            slots[0].Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
        }
    }

    private bool IsPointerOverElement(Vector2 pointerPosition, VisualElement element) {
        if (element == null) return false;

        var rect = element.worldBound;
        return rect.Contains(pointerPosition);
    }

    /// <summary>
    /// DEBUG ONLY: Button to simulate loot drop with predefined sprites
    /// </summary>
    private void OnLootButtonClicked()
    {
        if (debugLootSprites == null || debugLootSprites.Count == 0)
        {
            logger.Log("[DEBUG] No hay sprites asignados en debugLootSprites", this, Logging.LogType.Warning);
            return;
        }

        // Obtener el sprite actual de la lista
        Sprite spriteToAdd = debugLootSprites[currentLootIndex];
        
        // Avanzar al siguiente índice (con wrap-around)
        currentLootIndex = (currentLootIndex + 1) % debugLootSprites.Count;
        
        AddItemBySprite(spriteToAdd);
        logger.Log($"[DEBUG] Looteado sprite {currentLootIndex}/{debugLootSprites.Count}", this);
    }

    /// <summary>
    /// Add item to inventory by item ID (gets sprite from ItemsManager).
    /// This is the main method used by the game.
    /// </summary>
    public void AddItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            logger.Log("Cannot add item: itemId is null or empty", this, Logging.LogType.Warning);
            return;
        }

        if (ItemsManager == null)
        {
            logger.Log("ERROR: ItemsManager singleton not available! Make sure ItemsManager exists in MPMenuScene.", this, Logging.LogType.Error);
            return;
        }

        if (!ItemsManager.IsInitialized)
        {
            logger.Log("WARNING: ItemsManager not initialized yet, cannot add item", this, Logging.LogType.Warning);
            return;
        }

        // Get sprite from ItemsManager (coroutine for async loading)
        StartCoroutine(AddItemByIdCoroutine(itemId));
    }

    private System.Collections.IEnumerator AddItemByIdCoroutine(string itemId)
    {
        Texture2D texture = null;
        
        yield return ItemsManager.GetItemSprite(itemId, (loadedTexture) => {
            texture = loadedTexture;
        });

        if (texture != null)
        {
            // Convert Texture2D to Sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            AddItemBySprite(sprite);
            logger.Log($"Item added to inventory: {itemId}", this);
        }
        else
        {
            logger.Log($"Failed to load sprite for item: {itemId}", this, Logging.LogType.Error);
        }
    }

    /// <summary>
    /// Add item to inventory by sprite directly (used by debug button).
    /// </summary>
    public void AddItemBySprite(Sprite itemSprite)
    {
        if (itemSprite == null)
        {
            logger.Log("Cannot add item: sprite is null", this, Logging.LogType.Warning);
            return;
        }

        // Buscar el primer slot vacío
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                CreateItemElement(itemSprite, slot);
                logger.Log($"Item added to inventory", this);
                return;
            }
        }

        logger.Log("Inventory full, cannot add more items", this, Logging.LogType.Warning);
    }

    private void CreateItemElement(Sprite itemSprite, VisualElement parentSlot) {
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

    public bool IsInventoryFull() {
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                return false;
            }
        }
        return true;
    }

    public int GetEmptySlotCount()
    {
        logger?.Log($"[InventoryController] Contando slots vacíos. Total slots en lista: {slots.Count}", this);
        
        int count = 0;
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                count++;
            }
        }
        
        logger?.Log($"[InventoryController] Slots vacíos encontrados: {count}/{slots.Count}", this);
        return count;
    }

    public bool IsOpen() {
        return gameObject.activeSelf;
    }

    public void ToggleInventory() {
        logger.Log("Toggle settings", this);

        bool willBeActive = !gameObject.activeSelf;

        if (willBeActive) {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            logger.Log("Cursor shown (toggle)", this);
        } else {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            logger.Log("Cursor hidden (toggle)", this);
        }

        gameObject.SetActive(willBeActive);
    }
}
