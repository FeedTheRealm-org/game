using UnityEngine;

/// <summary>
/// Representa un ítem de loot que aparece en el mundo cuando un enemigo muere.
/// Por ahora solo es visual, sin interacción.
/// </summary>
public class LootItem : MonoBehaviour {
    
    [Header("Configuración del Loot")]
    [SerializeField]
    [Tooltip("El sprite o modelo visual que se mostrará en el piso")]
    private GameObject itemVisual;
    
    [SerializeField]
    [Tooltip("Nombre del ítem (opcional, para debug)")]
    private string itemName = "Loot Item";
    
    [SerializeField]
    [Tooltip("Offset vertical desde el punto de spawn")]
    private float heightOffset = 0.1f;
    
    [SerializeField]
    private Logging.Logger logger;

    private void Start() {
        // Ajustar la posición vertical si es necesario
        if (heightOffset != 0) {
            transform.position += Vector3.up * heightOffset;
        }
        
        // Verificar que hay un visual asignado
        if (itemVisual == null) {
            logger?.Log($"[LootItem] Warning: No visual asignado para el loot '{itemName}'", this, Logging.LogType.Warning);
        } else {
            logger?.Log($"[LootItem] Loot '{itemName}' spawneado en {transform.position}", this);
        }
    }

    /// <summary>
    /// Inicializa el loot item en una posición específica
    /// </summary>
    /// <param name="spawnPosition">Posición donde aparecerá el loot</param>
    public void Initialize(Vector3 spawnPosition) {
        transform.position = spawnPosition + Vector3.up * heightOffset;
    }
    
    /// <summary>
    /// Cambia el visual del loot en runtime (útil para diferentes tipos de loot)
    /// </summary>
    /// <param name="newVisual">El nuevo GameObject visual</param>
    public void SetVisual(GameObject newVisual) {
        // Destruir el visual anterior si existe
        if (itemVisual != null) {
            Destroy(itemVisual);
        }
        
        itemVisual = Instantiate(newVisual, transform);
        itemVisual.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Establece el nombre del ítem (útil para debug y futuras features)
    /// </summary>
    /// <param name="name">Nombre del ítem</param>
    public void SetItemName(string name) {
        itemName = name;
        gameObject.name = $"Loot_{name}";
    }

#if UNITY_EDITOR
    // Visualización en el editor para facilitar el debug
    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
#endif
}
