using UnityEngine;

/// <summary>
/// Componente que se agrega a los enemigos para que suelten loot al morir.
/// Se suscribe al evento OnDeath del HealthComponent.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class LootDropper : MonoBehaviour {
    
    [Header("Configuración de Loot")]
    [SerializeField]
    [Tooltip("Prefab del LootItem que se va a instanciar")]
    private GameObject lootPrefab;
    
    [SerializeField]
    [Tooltip("Si está activado, siempre dropeará loot. Si no, se puede usar probabilidad más adelante.")]
    private bool alwaysDrop = true;
    
    [SerializeField]
    [Tooltip("Offset de spawn del loot respecto a la posición del enemigo")]
    private Vector3 spawnOffset = Vector3.zero;
    
    [SerializeField]
    [Tooltip("Añade una variación aleatoria a la posición del spawn")]
    private float randomOffset = 0.5f;
    
    [SerializeField]
    private Logging.Logger logger;

    private HealthComponent healthComponent;

    private void Awake() {
        healthComponent = GetComponent<HealthComponent>();
        
        if (healthComponent == null) {
            logger?.Log("[LootDropper] Error: No se encontró HealthComponent en el enemigo!", this, Logging.LogType.Error);
            enabled = false;
            return;
        }
    }

    private void OnEnable() {
        if (healthComponent != null) {
            healthComponent.OnDeath += HandleDeath;
        }
    }

    private void OnDisable() {
        if (healthComponent != null) {
            healthComponent.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Se ejecuta cuando el enemigo muere
    /// </summary>
    private void HandleDeath() {
        if (alwaysDrop && lootPrefab != null) {
            DropLoot();
        } else if (lootPrefab == null) {
            logger?.Log("[LootDropper] Warning: No hay prefab de loot asignado!", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Instancia el loot en la posición del enemigo
    /// </summary>
    private void DropLoot() {
        // Calcular posición de spawn con offset aleatorio
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        if (randomOffset > 0) {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            spawnPosition += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        // Instanciar el loot
        GameObject lootInstance = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
        
        // Inicializar el LootItem si tiene el componente
        LootItem lootItem = lootInstance.GetComponent<LootItem>();
        if (lootItem != null) {
            lootItem.Initialize(spawnPosition);
        }
        
        logger?.Log($"[LootDropper] Loot dropeado en {spawnPosition}", this);
    }

#if UNITY_EDITOR
    // Visualización en el editor para ver dónde caerá el loot
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Vector3 dropPosition = transform.position + spawnOffset;
        Gizmos.DrawWireSphere(dropPosition, 0.2f);
        
        // Mostrar el área de spawn aleatorio
        if (randomOffset > 0) {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(dropPosition, randomOffset);
        }
    }
#endif
}
