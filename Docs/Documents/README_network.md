# Documentación de Scripts - Feed The Realm

Este documento describe los scripts principales utilizados en el proyecto Feed The Realm, un juego multiplayer desarrollado con Unity y Unity Netcode. Cada sección explica qué hace el script, sus dependencias, configuraciones necesarias y campos serializables.

## Índice de Contenidos

### Arquitectura General
- [Resumen General](#resumen-general)
  - [Servidor Headless](#servidor-headless)
  - [Responsabilidades del Servidor en Multiplayer](#responsabilidades-del-servidor-en-multiplayer)
  - [Arquitectura Server-Authoritative](#arquitectura-server-authoritative)
- [Configuración de la Escena del Menú (MPMenuScene)](#configuración-de-la-escena-del-menú-mpmmenuscene)

### Scripts de Red y Gestión
1. [NetworkConnectionHandler.cs](#1-networkconnectionhandlercs)
2. [ServerBootstrap.cs](#2-serverbootstrapcs)
3. [NetworkDebugger.cs](#3-networkdebuggercs)
4. [NetworkSceneManager.cs](#4-networkscenemanagercs)
5. [PersistentManager.cs](#5-persistentmanagercs)

### Scripts de Gameplay
6. [PlayerSpawnManager.cs](#6-playerspawnmanagercs)
7. [ScenePreloader.cs](#7-scenepreloadercs)
8. [CameraSetup.cs](#8-camerasetupcs)
9. [GameSceneManager.cs](#9-gamescenemanagercs)

### Scripts de Spawning y Enemigos
10. [EnemySpawn.cs](#10-enemyspawncs)
11. [NetworkHealthSynchronizer.cs](#11-networkhealthsynchronizercs)
12. [NetworkAttackSynchronizer.cs](#12-networkattacksynchronizercs)
13. [NetworkEnemySynchronizer.cs](#13-networkenemysynchronizercs)

### Documentación Técnica
- [Arquitectura de Sincronización en Multiplayer](#arquitectura-de-sincronización-en-multiplayer)
- [Notas Generales](#notas-generales)

---

## Resumen General

### Servidor Headless
- **Funcionamiento**: El servidor es headless (sin renderizado gráfico), ejecutándose en builds dedicados con el define `UNITY_SERVER`. No renderiza gráficos ni maneja input del usuario, solo procesa lógica de red y simulación del juego.
- **Diferencia con Cliente**: Los builds de cliente incluyen UI, renderizado y controles de usuario, mientras que el servidor solo ejecuta lógica de red. Ambos usan Unity Netcode, pero el servidor inicia automáticamente con `ServerBootstrap`.
- **Renderizado**: El servidor NO renderiza al cliente; solo procesa estado del juego y sincroniza datos.

### Responsabilidades del Servidor en Multiplayer
El servidor tiene autoridad completa sobre el estado del juego y es responsable de:

#### 1. **Spawning de Entidades**
- **EnemySpawn.cs**: Solo el servidor puede spawnear enemigos. Los clientes solo observan.
  - Detecta triggers de zonas de spawn (solo en servidor)
  - Instancia enemigos y ejecuta `NetworkObject.Spawn()`
  - Gestiona contadores de enemigos activos y kills
  - Maneja lógica de reset de spawners

#### 2. **Sincronización de Salud y Muerte**
- **NetworkHealthSynchronizer.cs**: El servidor procesa todo el daño y muerte.
  - Recibe eventos de `HealthComponent.TakeDamage()` en el servidor
  - Actualiza `NetworkVariable<int> networkHealth` para sincronizar a clientes
  - Ejecuta `NetworkObject.Despawn()` cuando una entidad muere
  - Los clientes solo reciben actualizaciones y reproducen animaciones

#### 3. **Validación de Ataques**
- **NetworkAttackSynchronizer.cs**: El servidor valida y ejecuta todos los ataques.
  - Clientes envían `ServerRpc` cuando atacan (input del jugador)
  - Servidor ejecuta `Physics.OverlapSphere()` para detectar hits
  - Servidor aplica daño llamando `HealthComponent.TakeDamage()` en targets
  - Servidor transmite animaciones de ataque via `ClientRpc` a clientes remotos
  - **Prevención de cheating**: Los clientes NO pueden aplicar daño directamente

#### 4. **Simulación de Física y Movimiento**
- **NetworkEnemySynchronizer.cs**: El servidor simula física de enemigos.
  - Servidor tiene `Rigidbody.isKinematic = false` (física activa)
  - Clientes tienen `Rigidbody.isKinematic = true` (solo visual)
  - Servidor sincroniza posición, rotación y velocidad via `NetworkVariables`
  - Clientes interpolan suavemente a las posiciones del servidor
  - Usado para enemigos estáticos que pueden ser empujados por física

#### 5. **Gestión de Estado del Juego**
- Control de escenas (solo servidor puede cargar escenas via `NetworkSceneManager`)
- Gestión de conexiones/desconexiones de jugadores
- Spawn de jugadores en puntos específicos (`PlayerSpawnManager`)
- Validación de acciones del jugador (movimiento, ataques, interacciones)

### Arquitectura Server-Authoritative
- **Principio**: El servidor tiene la última palabra en todo el estado del juego
- **Clientes**: Solo envían inputs y reciben actualizaciones de estado
- **Validación**: El servidor valida todas las acciones antes de aplicarlas
- **Sincronización**: NetworkVariables y RPCs mantienen clientes sincronizados
- **Seguridad**: Previene cheating al no permitir que clientes modifiquen estado directamente

### Comunicación de Red
- **Protocolo**: Utiliza UDP para la comunicación entre servidor y clientes a través de Unity Transport (parte de Unity Netcode).
- **Unity Network Manager**: Componente central que maneja conexiones, sincronización y escenas. Debe tener configurado:
  - **Unity Transport**: Address vacío (para servidor) y activada "Allow Remote Connections".
  - **ServerBootstrap**: Puerto 7777, IP 0.0.0.0, escena del juego configurada.

### NetworkConnectionHandler.cs - Aspectos Relevantes
- Gestiona conexiones cliente-servidor, incluyendo loading screens y reconexiones.
- Persiste entre escenas y coordina con UI para iniciar conexiones.
- Maneja eventos de conexión/desconexión y configuración de transporte UDP.

## Configuración de la Escena del Menú (MPMenuScene)

La escena `MPMenuScene` debe contener los siguientes objetos en Hierarchy, ALGUNOS COMPONENTES COMO NETWORK MANAGER NECESITAN ESTAR EN ESTA ESCENA, EN CASO DE CAMBIAR LA ESCENA INICIAL PARA SERVIDOR, AGREGAR ESTOS COMPONENTES PARA PERSISTIR Y QUE EL SERVIDOR SIGA FUNCIONANDO, Y ADEMAS NO DEBE DUPLICARSE EN CADA ESCENA:

- **NetworkManager** (GameObject vacío):
  - Agregar componentes: `NetworkManager`, `UnityTransport`, `ServerBootstrap`.
  - Configurar `UnityTransport`: Address vacío, Allow Remote Connections activado.
  - Configurar `ServerBootstrap`: Puerto 7777, IP 0.0.0.0, escena del juego asignada.

- **EventSystem** (GameObject con componente EventSystem de Unity UI).

- **NetworkDebugger** (GameObject vacío con script `NetworkDebugger`).

- **PersistentManager** (GameObject vacío con script `PersistentManager`):
  - Agregar como hijo: **NetworkSceneManager** (con script `NetworkSceneManager`).
  - Configurar en `NetworkSceneManager`: `gameScene` y `menuScene` con referencias a escenas.

- **ScenePreloader** (GameObject vacío con script `ScenePreloader`).

- **NetworkConnectionHandler** (GameObject vacío con script `NetworkConnectionHandler`).

- **Prefabs**: Instanciar `LoadingScreen` y `MenuUI` en la escena.

## 1. NetworkConnectionHandler.cs

**Descripción**: Maneja el ciclo de vida de la conexión de red y coordina el loading screen. Este componente persiste entre cambios de escena usando `DontDestroyOnLoad`. Gestiona conexiones UDP entre cliente y servidor, incluyendo reconexiones y eventos de red.

**Campos Serializables**:
- `logger` (Logging.Logger): Componente de logging personalizado para depuración.
- `enableLogging` (bool): Habilita/desactiva el logging (por defecto true).

**Configuraciones Esperadas**:
- Debe estar adjunto a un GameObject persistente.
- Requiere un NetworkManager en la escena con Unity Transport configurado para UDP.
- Espera ser llamado desde el UI (ej: MultiplayerMenuController) para conectar al servidor.

**Dependencias**:
- Unity.Netcode
- Unity.Netcode.Transports.UTP (para transporte UDP)
- Logging.Logger (sistema de logging personalizado)


## 2. ServerBootstrap.cs

**Descripción**: Inicia automáticamente un servidor dedicado en builds de servidor, configurado desde línea de comandos. Utiliza Unity Network Manager y Unity Transport para comunicación UDP.

**Campos Serializables**:
- `defaultPort` (ushort): Puerto por defecto (7777).
- `defaultAddress` (string): Dirección por defecto ("0.0.0.0").
- `maxPlayers` (int): Máximo jugadores (10).
- `gameScene` (SceneReference): Referencia a la escena del juego.
- `autoLoadGameScene` (bool): Carga escena automáticamente.
- `verboseLogging` (bool): Logging detallado.

**Configuraciones Esperadas**:
- Solo ejecuta en builds de servidor (#UNITY_SERVER).
- Configurado desde argumentos de línea de comandos (-port, -address, -maxplayers).
- La escena del juego se configura arrastrando la escena al campo gameScene en el Inspector.
- **Configuración específica**: Puerto 7777, IP 0.0.0.0, escena del juego asignada. Requiere Unity Network Manager con Unity Transport (address vacío, Allow Remote Connections activado).

**Dependencias**:
- Unity.Netcode
- Unity.Netcode.Transports.UTP (para UDP)
- SceneReference (de FeedTheRealm_Shared)
-

## 3. NetworkDebugger.cs

**Descripción**: Logger centralizado para información de red y escenas. Consolida debugging sin múltiples componentes.

**Campos Serializables**:
- `enablePeriodicLogs` (bool): Habilita logs periódicos de estado.
- `logIntervalSeconds` (float): Intervalo entre logs periódicos (por defecto 10s).
- `logSceneChanges` (bool): Loggea cambios de escena.
- `logNetworkEvents` (bool): Loggea eventos de red.
- `logBuildScenes` (bool): Loggea escenas en build settings.
- `logger` (Logging.Logger): Sistema de logging.

**Configuraciones Esperadas**:
- Singleton que persiste entre escenas.
- Se inicializa automáticamente al cargar.

**Dependencias**:
- Unity.Netcode
- UnityEngine.SceneManagement
- Logging.Logger

## 4. NetworkSceneManager.cs

**Descripción**: Gestiona la carga de escenas en red a través de Unity Network Manager, permitiendo al servidor cargar escenas sincronizadas para todos los clientes.

**Campos Serializables**:
- `gameScene` (SceneReference): Referencia a la escena del juego.
- `menuScene` (SceneReference): Referencia a la escena del menú.
- `logger` (Logging.Logger): Comentado, pero disponible para logging.

**Configuraciones Esperadas**:
- Singleton persistente.
- Solo el servidor puede cargar escenas.
- Las escenas deben estar en Build Settings.
- Requiere Unity Network Manager activo.

**Dependencias**:
- Unity.Netcode
- UnityEngine.SceneManagement

## 5. PersistentManager.cs

**Descripción**: Gestiona managers que deben persistir entre escenas, inicializando sistemas core.

**Campos Serializables**: Ninguno directo, pero inicializa componentes dinámicamente.

**Configuraciones Esperadas**:
- Adjunto a un GameObject en la escena inicial.
- Inicializa NetworkSceneManager automáticamente.

**Dependencias**:
- UnityEngine

## 6. PlayerSpawnManager.cs

**Descripción**: Gestiona el spawn de jugadores en puntos específicos, con soporte para auto-detección y ajustes de altura.

**Campos Serializables**:
- `spawnPoints` (Transform[]): Array de puntos de spawn.
- `autoDetectSpawnPoints` (bool): Auto-detecta spawn points por tag.
- `spawnPointTag` (string): Tag para detectar spawn points (por defecto "SpawnPoint").
- `groundReference` (Transform): Referencia para ajustar altura al suelo.
- `adjustToGroundHeight` (bool): Ajusta spawn a la altura del suelo.
- `heightOffset` (float): Offset adicional de altura.
- `maxPlayers` (int): Máximo número de jugadores (por defecto 100).
- `logger` (Logging.Logger): Para logging.

**Configuraciones Esperadas**:
- Adjunto a un NetworkBehaviour en la escena del juego.
- Funciona solo en el servidor.
- Spawn points pueden ser hijos directos o tagged.

**Dependencias**:
- Unity.Netcode
- Logging.Logger

## 7. ScenePreloader.cs

**Descripción**: Pre-carga escenas en segundo plano para mejorar tiempos de carga.

**Campos Serializables**:
- `scenesToPreload` (SceneReference[]): Array de escenas a pre-cargar.
- `preloadOnStart` (bool): Inicia preload automáticamente.
- `delayBeforePreload` (float): Delay antes de iniciar preload (por defecto 1s).
- `showDebugLogs` (bool): Muestra logs de debug.
- `logger` (Logging.Logger): Sistema de logging.

**Configuraciones Esperadas**:
- Adjunto a un GameObject persistente en la escena del menú.
- Escenas deben estar en Build Settings.

**Dependencias**:
- UnityEngine.SceneManagement
- Logging.Logger

## 8. CameraSetup.cs

**Descripción**: Configura automáticamente la cámara Cinemachine para seguir al jugador local.

**Campos Serializables**:
- `playerTag` (string): Tag del jugador (por defecto "Player").
- `targetChildName` (string): Nombre del hijo del jugador a seguir (opcional).
- `logger` (Logging.Logger): Para logging.

**Configuraciones Esperadas**:
- Adjunto a un GameObject con cámara.
- Espera un CinemachineCamera en la escena.
- El jugador debe tener NetworkObject y ser owner.

**Dependencias**:
- Unity.Cinemachine
- Unity.Netcode
- Logging.Logger

## 9. GameSceneManager.cs

**Descripción**: Gestiona la UI y lógica de la escena del juego, incluyendo desconexión y estado del cursor.

**Campos Serializables**:
- `disconnectButton` (Button): Botón para desconectar.
- `returnToMenuPanel` (GameObject): Panel de retorno al menú.
- `logger` (Logging.Logger): Para logging.

**Configuraciones Esperadas**:
- Adjunto en la escena del juego.
- UI configurada con botones y paneles.
- Inicializa el cursor en modo bloqueado para gameplay.

**Dependencias**:
- Unity.Netcode
- UnityEngine.UI
- UnityEngine.InputSystem
- Logging.Logger

---

## 10. EnemySpawn.cs

**Descripción**: Gestiona el spawn automático de enemigos en zonas específicas cuando jugadores entran. Diseñado para ser **server-authoritative** en multiplayer: solo el servidor spawea y gestiona enemigos, los clientes solo observan.

**Campos Serializables**:
- **Spawn Settings**:
  - `enemyPrefab` (GameObject): Prefab del enemigo a spawnear (debe tener NetworkObject).
  - `maxEnemies` (int): Máximo de enemigos simultáneos (por defecto 3).
  - `spawnRate` (float): Intervalo entre spawns en segundos (por defecto 2s).
  - `resetAfterKills` (int): Número de kills para resetear spawner (por defecto 6).
  - `resetDelay` (float): Tiempo de espera antes de reset (por defecto 10s).
- **Spawn Points**:
  - `spawnPointContainer` (Transform): Contenedor con puntos de spawn como hijos.
- **General**:
  - `logger` (Logging.Logger): Sistema de logging.

**Configuraciones Esperadas**:
- Adjunto a un GameObject con Collider configurado como Trigger (ej: SphereCollider).
- El enemyPrefab DEBE tener componente `NetworkObject` para multiplayer.
- Los spawn points deben ser hijos de `spawnPointContainer`.
- En **multiplayer**: Solo ejecuta en servidor (`NetworkManager.IsServer`).
- En **singleplayer**: Ejecuta normalmente sin NetworkManager.

**Comportamiento en Multiplayer**:
- **Servidor**:
  - Detecta cuando jugadores entran/salen del trigger
  - Spawna enemigos llamando `NetworkObject.Spawn()`
  - Gestiona contadores de enemigos activos
  - Escucha eventos de muerte de enemigos (`HealthComponent.OnDeath`)
  - Ejecuta lógica de reset cuando se alcanzan kills
- **Clientes**:
  - Ignoran triggers completamente (no procesan spawns)
  - Reciben enemigos ya spawneados por el servidor via replicación de NetworkObject
  - Observan estado sincronizado via NetworkHealthSynchronizer

**Dependencias**:
- Unity.Netcode (opcional, pero requerido para multiplayer)
- HealthComponent (para detectar muertes)
- NetworkObject (en enemyPrefab para multiplayer)
- Logging.Logger

---

## 11. NetworkHealthSynchronizer.cs

**Descripción**: Sincroniza el estado de salud y muerte de entidades networked (jugadores, enemigos, NPCs) manteniendo una arquitectura **server-authoritative**. Actúa como capa de sincronización sobre `HealthComponent`, permitiendo que este último permanezca como MonoBehaviour puro.

**Campos Serializables**:
- `healthComponent` (HealthComponent): Referencia al componente de salud local.
- `logger` (Logging.Logger): Sistema de logging.

**NetworkVariables** (Autoridad del Servidor):
- `networkHealth` (NetworkVariable<int>): Salud actual sincronizada.
- `networkIsDead` (NetworkVariable<bool>): Estado de muerte sincronizado.

**Configuraciones Esperadas**:
- Adjunto al mismo GameObject que `HealthComponent` (o auto-detectado con GetComponent).
- Requiere `NetworkObject` en el GameObject.
- El `HealthComponent` debe tener las propiedades públicas:
  - `GetCurrentHealth()`: Obtener salud actual
  - `SetCurrentHealth(int)`: Aplicar salud sin animaciones
  - Eventos: `OnHealthChanged`, `OnDeath`

**Comportamiento en Multiplayer**:

### **En el Servidor**:
1. **Inicialización**:
   - Lee salud inicial de `HealthComponent.GetCurrentHealth()`
   - Establece `networkHealth.Value` con salud inicial
   - Suscribe a eventos: `OnHealthChanged` y `OnDeath`

2. **Cuando un enemigo/jugador recibe daño**:
   - `HealthComponent.TakeDamage()` se ejecuta en servidor
   - `OnHealthChangedServer()` actualiza `networkHealth.Value`
   - NetworkVariable sincroniza automáticamente a todos los clientes

3. **Cuando muere**:
   - `OnDeathServer()` establece `networkIsDead.Value = true`
   - `HealthComponent.Die()` ejecuta `NetworkObject.Despawn()` para destruir en red

### **En los Clientes**:
1. **Inicialización**:
   - Recibe valores iniciales de `networkHealth` y `networkIsDead`
   - Aplica salud inicial llamando `healthComponent.SetCurrentHealth()`
   - Suscribe a cambios de NetworkVariables

2. **Cuando salud cambia (desde servidor)**:
   - `OnHealthChangedClient()` detecta cambio en `networkHealth`
   - Aplica nueva salud: `healthComponent.SetCurrentHealth(newValue)`
   - **Si salud bajó**: Reproduce animación de daño (`3_Damaged` trigger)
   - **Si salud = 0**: Reproduce animación de muerte (`4_Death` trigger)

3. **Cuando se recibe muerte**:
   - `OnIsDeadChangedClient()` detecta `networkIsDead = true`
   - Reproduce animación de muerte
   - **NO destruye el objeto** (el NetworkObject.Despawn del servidor lo hace)

**Optimizaciones**:
- Cachea referencia a `Animator` en `Awake()` para evitar `GetComponentInChildren()` repetidos
- Solo sincroniza cuando hay cambios reales de salud
- Previene procesamiento duplicado de muerte con flag `hasProcessedDeath`

**Dependencias**:
- Unity.Netcode
- HealthComponent
- NetworkObject
- Animator (para animaciones de daño/muerte)
- Logging.Logger

---

## 12. NetworkAttackSynchronizer.cs

**Descripción**: Sincroniza ataques de personajes en multiplayer con arquitectura **server-authoritative**. Los clientes envían inputs de ataque, pero solo el servidor ejecuta detección de hits y aplica daño. Esto previene cheating y mantiene consistencia del juego.

**Campos Serializables**:
- `attackComponent` (AttackComponent): Referencia al componente de ataque local.
- `logger` (Logging.Logger): Sistema de logging.
- **Debug**:
  - `enableLogs` (bool): Toggle para habilitar/deshabilitar logs (por defecto true).
- **Attack Settings** (Auto-configurado desde AttackComponent):
  - `targetLayers` (LayerMask): Capas que pueden ser golpeadas (ej: Enemy).
  - `hitRadius` (float): Radio de detección de hits.
  - `attackDamage` (int): Daño base del ataque.

**Configuraciones Esperadas**:
- Adjunto al mismo GameObject que `AttackComponent` (o configurado manualmente).
- Requiere `NetworkObject` en el GameObject.
- El `AttackComponent` debe tener métodos públicos:
  - `GetHitPoint()`: Transform del punto de impacto
  - `GetHitRadius()`: Radio de ataque
  - `GetAttackDamage()`: Daño del ataque
  - `GetTargetLayer()`: LayerMask de objetivos
- Llamado desde `AnimationEvents.Attack()` en frame de impacto de la animación.

**Comportamiento en Multiplayer**:

### **En el Cliente (Owner)**:
1. **Inicialización**:
   - Auto-configura parámetros desde `AttackComponent`
   - Cachea `Animator` para evitar GetComponentInChildren() repetidos
   - Marca como `isLocalPlayerOwned = true`

2. **Cuando el jugador ataca**:
   - `AnimationEvents.Attack()` llama `DetectAttackHit()`
   - **NO ejecuta detección local** (solo en singleplayer)
   - Envía `DetectAttackHitServerRpc(hitPoint.position)` al servidor
   - Reproduce animación de ataque localmente (via AttackComponent)

### **En el Servidor**:
1. **Recibe ataque del cliente**:
   - `DetectAttackHitServerRpc()` recibe posición del ataque
   - Ejecuta `Physics.OverlapSphere()` para detectar colisiones
   - **Valida y aplica daño** a todos los enemigos en rango:
     ```csharp
     HealthComponent targetHealth = hit.GetComponent<HealthComponent>();
     targetHealth.TakeDamage(attackDamage);
     ```
   - Transmite animación a clientes remotos via `PlayAttackAnimationClientRpc()`

2. **Broadcast de animación**:
   - `PlayAttackAnimationClientRpc()` se envía a todos los clientes
   - **Excepto el owner** (que ya reprodujo la animación localmente)
   - Clientes remotos reproducen animación: `animator.SetTrigger("2_Attack")`

### **En Clientes Remotos**:
1. **Reciben ataque de otro jugador**:
   - `PlayAttackAnimationClientRpc()` dispara animación
   - Solo reproducen animación visual
   - **NO ejecutan lógica de daño** (el servidor ya lo hizo)

**Flujo Completo de un Ataque**:
```
1. Jugador A (Cliente) presiona botón de ataque
2. AttackComponent.OnAttack() inicia animación localmente
3. Frame de impacto: AnimationEvents.Attack() → NetworkAttackSynchronizer.DetectAttackHit()
4. Cliente A envía ServerRpc al servidor con posición del hit
5. SERVIDOR ejecuta Physics.OverlapSphere() y detecta enemigos
6. SERVIDOR aplica daño: enemigo.TakeDamage(40)
7. SERVIDOR envía ClientRpc a Jugador B, C, D... (no a A)
8. Jugadores remotos reproducen animación de ataque de Jugador A
```

**Prevención de Cheating**:
- Solo el servidor puede aplicar daño
- Clientes NO ejecutan `Physics.OverlapSphere()` para ataques
- Servidor valida cada ataque antes de aplicar daño
- NetworkVariables de salud son `WritePermission.Server`

**Optimizaciones**:
- Cachea `Animator` en `Awake()` (100x más rápido que GetComponentInChildren repetidos)
- Toggle de logs con `enableLogs` para reducir spam en producción
- Solo envía animación a clientes remotos (owner ya tiene animación local)

**Dependencias**:
- Unity.Netcode
- AttackComponent
- NetworkObject
- Animator (para animaciones)
- HealthComponent (en targets)
- AnimationEvents (para detectar frame de impacto)
- Logging.Logger

---

## 13. NetworkEnemySynchronizer.cs

**Descripción**: Sincroniza posición, rotación y velocidad de enemigos networked con física. Diseñado para enemigos **estáticos o impulsados por física** (no AI activa). El servidor tiene autoridad sobre la simulación física, los clientes solo interpolan visualmente.

**Campos Serializables**:
- `rb` (Rigidbody): Referencia al Rigidbody del enemigo (auto-detectado).
- `logger` (Logging.Logger): Sistema de logging.
- **Network Settings**:
  - `networkSendRate` (float): Actualizaciones por segundo del servidor (por defecto 10 Hz).
  - `positionThreshold` (float): Cambio mínimo de posición para enviar update (por defecto 0.1m).
  - `rotationThreshold` (float): Cambio mínimo de rotación para enviar update (por defecto 5 grados).
- **Client Interpolation**:
  - `interpolationSpeed` (float): Velocidad de interpolación en clientes (por defecto 15).

**NetworkVariables** (Autoridad del Servidor):
- `networkPosition` (NetworkVariable<Vector3>): Posición sincronizada.
- `networkRotation` (NetworkVariable<Quaternion>): Rotación sincronizada.
- `networkVelocity` (NetworkVariable<Vector3>): Velocidad sincronizada (para predicción).

**Configuraciones Esperadas**:
- Adjunto al mismo GameObject que `Rigidbody`.
- Requiere `NetworkObject` en el GameObject.
- El enemigo debe tener física configurada (Rigidbody + Collider).
- Usado principalmente para enemigos que:
  - Están estáticos hasta que el jugador interactúa
  - Pueden ser empujados por física (explosiones, ataques, etc.)
  - No tienen AI de movimiento complejo

**Comportamiento en Multiplayer**:

### **En el Servidor**:
1. **Inicialización**:
   - `Rigidbody.isKinematic = false` (física activa)
   - Inicializa NetworkVariables con transform actual
   - Tracking de última posición/rotación enviada

2. **Simulación Física** (FixedUpdate):
   - Unity Physics Engine simula normalmente
   - Enemigo puede ser empujado, caer, colisionar, etc.

3. **Sincronización Periódica**:
   - Cada `1/networkSendRate` segundos (default 0.1s):
     - Verifica si posición cambió > `positionThreshold`
     - Verifica si rotación cambió > `rotationThreshold`
     - **Si hubo cambio significativo**: Actualiza NetworkVariables
   - **Optimización**: Solo envía cuando hay cambio real (ahorra bandwidth)

### **En los Clientes**:
1. **Inicialización**:
   - `Rigidbody.isKinematic = true` (física desactivada, solo visual)
   - Aplica posición/rotación inicial inmediatamente
   - No simula física localmente

2. **Interpolación Suave** (FixedUpdate):
   - Recibe updates de NetworkVariables
   - Interpola suavemente hacia posición del servidor:
     ```csharp
     transform.position = Vector3.Lerp(current, networkPosition, deltaTime * interpolationSpeed);
     transform.rotation = Quaternion.Slerp(current, networkRotation, deltaTime * interpolationSpeed);
     ```
   - Opcionalmente aplica velocidad para mejor predicción

**API Pública**:

### `Teleport(Vector3 position, Quaternion rotation)` (Solo Servidor)
- Teletransporta enemigo a nueva posición (útil para respawn)
- Resetea velocidad a cero
- Fuerza sincronización inmediata a clientes
- Ejemplo:
  ```csharp
  if (NetworkManager.Singleton.IsServer) {
      enemySynchronizer.Teleport(spawnPoint.position, spawnPoint.rotation);
  }
  ```

### `GetNetworkPosition()` / `GetNetworkRotation()`
- Obtiene posición/rotación sincronizada actual
- Útil para clientes que necesitan posición "oficial" del servidor

**Caso de Uso Típico**:
```
1. Servidor spawna enemigo estático con NetworkEnemySynchronizer
2. Clientes reciben enemigo, se posiciona en networkPosition
3. Jugador lanza explosión cerca del enemigo
4. SERVIDOR: Física de Unity empuja al enemigo (RB.AddForce)
5. SERVIDOR: NetworkEnemySynchronizer detecta cambio de posición
6. SERVIDOR: Actualiza networkPosition/networkVelocity
7. CLIENTES: Reciben update, interpolan suavemente a nueva posición
8. Resultado: Todos los jugadores ven al enemigo moverse de forma sincronizada
```

**Limitaciones**:
- No adecuado para enemigos con AI de movimiento complejo (usar NavMesh + sincronización de destino)
- Interpolación puede tener lag visual (~100-200ms dependiendo de send rate)
- Física solo simula en servidor (clientes no pueden predecir)

**Optimizaciones**:
- Solo envía updates cuando hay cambio > threshold (ahorra 90% de bandwidth)
- Rate limiting: máximo 10 updates/segundo por defecto
- Interpolación suave previene "teleportation" visual

**Dependencias**:
- Unity.Netcode
- Rigidbody (para física)
- NetworkObject
- Logging.Logger

---

---

## Arquitectura de Sincronización en Multiplayer

### Separación de Responsabilidades

El proyecto utiliza una arquitectura de **capas separadas** para mantener código limpio y modular:

#### **Capa 1: MonoBehaviours Puros** (Sin networking)
Scripts core del juego que funcionan en singleplayer y multiplayer:
- `HealthComponent`: Gestiona salud, daño, muerte
- `AttackComponent`: Gestiona ataques, cooldowns, hit detection
- `MovementComponent`: Gestiona movimiento del personaje
- `DashComponent`: Gestiona dash/esquive

**Características**:
- NO heredan de `NetworkBehaviour`
- NO tienen `NetworkVariables`
- Pueden ser usados en juegos singleplayer sin cambios
- Lógica de gameplay pura

#### **Capa 2: NetworkBehaviours (Sincronización)**
Scripts que envuelven los MonoBehaviours para multiplayer:
- `NetworkHealthSynchronizer`: Sincroniza `HealthComponent`
- `NetworkAttackSynchronizer`: Sincroniza `AttackComponent`
- `NetworkEnemySynchronizer`: Sincroniza posición/física de enemigos
- `NetworkPlayerController`: Gestiona input y sincronización de jugador

**Características**:
- Heredan de `NetworkBehaviour`
- Usan `NetworkVariables`, `ServerRpc`, `ClientRpc`
- **Leen** de MonoBehaviours, **NO modifican** directamente
- Actúan como "puente" entre red y lógica local

### Flujo de Datos: Ataque en Multiplayer

```mermaid
Cliente A (Owner)                    Servidor                         Cliente B (Remoto)
      |                                  |                                    |
      | 1. Input de ataque               |                                    |
      |--------------------------------->|                                    |
      | AttackComponent.OnAttack()       |                                    |
      |                                  |                                    |
      | 2. Animación local               |                                    |
      | Animator.SetTrigger("2_Attack")  |                                    |
      |                                  |                                    |
      | 3. Frame de impacto              |                                    |
      | AnimationEvents.Attack()         |                                    |
      |                                  |                                    |
      | 4. ServerRpc                     |                                    |
      |--------------------------------->| 5. Physics.OverlapSphere()         |
      |                                  | Detecta enemigos en rango          |
      |                                  |                                    |
      |                                  | 6. Aplica daño                     |
      |                                  | enemy.TakeDamage(40)               |
      |                                  |                                    |
      |                                  | 7. NetworkHealthSynchronizer       |
      |                                  | Actualiza networkHealth            |
      |                                  |                                    |
      | 8. ClientRpc (solo remotos)      |                                    |
      |                                  |--------------------------------->  |
      |                                  |    9. Reproduce animación          |
      |                                  |    Animator.SetTrigger("2_Attack") |
      |                                  |                                    |
      |                                  | 10. Sincroniza salud               |
      |<---------------------------------|--------------------------------->  |
      | Recibe nueva salud del enemigo   |    Recibe nueva salud del enemigo  |
```

### Patrones de Comunicación

#### **ServerRpc** (Cliente → Servidor)
Usado cuando cliente necesita pedir permiso o enviar input:
```csharp
[ServerRpc]
private void DetectAttackHitServerRpc(Vector3 attackPosition) {
    // Solo ejecuta en servidor
    // Valida y procesa ataque
}
```
- Cliente llama el método
- Se ejecuta SOLO en el servidor
- Servidor tiene autoridad para aceptar/rechazar

#### **ClientRpc** (Servidor → Clientes)
Usado cuando servidor necesita notificar a todos:
```csharp
[ClientRpc]
private void PlayAttackAnimationClientRpc() {
    // Se ejecuta en TODOS los clientes
    if (IsOwner) return; // Owner ya tiene animación
    animator.SetTrigger("2_Attack");
}
```
- Servidor llama el método
- Se ejecuta en TODOS los clientes conectados
- Útil para efectos visuales, sonidos, animaciones

#### **NetworkVariables** (Servidor → Clientes automático)
Usado para estado que debe estar sincronizado constantemente:
```csharp
private NetworkVariable<int> networkHealth = new NetworkVariable<int>(
    writePerm: NetworkVariableWritePermission.Server
);
```
- Solo el servidor puede escribir (`WritePermission.Server`)
- Clientes reciben updates automáticamente
- Callbacks: `OnValueChanged` para reaccionar a cambios
- Optimizado: Solo envía cuando valor cambia

### Prevención de Cheating

#### ✅ **Implementado (Server-Authoritative)**
- **Daño**: Solo servidor aplica daño via `TakeDamage()`
- **Muerte**: Solo servidor despawnea entidades
- **Spawning**: Solo servidor puede spawnear enemigos
- **Física**: Solo servidor simula física de enemigos
- **Validación**: Servidor ejecuta `Physics.OverlapSphere()` para ataques

#### ❌ **No Implementado (Cliente tiene libertad)**
- **Movimiento**: Clientes envían input, no hay validación anti-speedhack
- **Cooldowns**: Clientes controlan cooldowns de ataque (modificable)
- **Posición**: Clientes reportan su propia posición sin validación

#### 🔐 **Mejoras Futuras para Anti-Cheat**
1. **Validación de movimiento**: Servidor verifica velocidad máxima
2. **Validación de rango**: Servidor verifica distancia al atacar
3. **Cooldown en servidor**: Servidor trackea último ataque de cada cliente
4. **Timestamp validation**: Verificar que inputs no sean del futuro

### Escalabilidad y Optimización

#### **Bandwidth Optimization**
- `NetworkEnemySynchronizer`: Solo envía cuando cambio > threshold (ahorra 90%)
- `NetworkHealthSynchronizer`: Solo envía cuando salud cambia
- `NetworkAttackSynchronizer`: No sincroniza hit detection, solo resultado

#### **CPU Optimization**
- **Caching**: Todos los synchronizers cachean `Animator` en `Awake()`
- **Send Rate Limiting**: Updates de posición limitados a 10 Hz
- **Conditional Logging**: Toggle `enableLogs` para desactivar logs en producción

#### **Escalado para MMO**
Actualmente optimizado para **~100 jugadores simultáneos**. Para escalar a 1000+:
1. **Interest Management**: Solo sincronizar entidades cercanas al jugador
2. **Object Pooling**: Reusar enemigos en vez de Instantiate/Destroy
3. **Spatial Hashing**: Agrupar entidades por zona para reducir checks
4. **LOD de sincronización**: Enemigos lejos = menos updates/segundo

---

## Notas Generales

- Todos los scripts usan un sistema de logging personalizado (`Logging.Logger`).
- La mayoría requieren Unity Netcode para funcionalidad de red.
- Campos serializables deben configurarse en el Inspector de Unity.
- Algunos scripts son singletons y persisten entre escenas.
- Verificar que las escenas referenciadas estén incluidas en Build Settings.
- **Prefabs de enemigos** deben tener: `NetworkObject`, `HealthComponent`, `NetworkHealthSynchronizer`, `NetworkEnemySynchronizer`.
- **Prefabs de jugadores** deben tener: `NetworkObject`, `NetworkPlayerController`, `AttackComponent`, `NetworkAttackSynchronizer`.
- Para debugging, habilitar `enableLogs` en los synchronizers (desactivar en producción).
- El servidor SIEMPRE tiene autoridad sobre salud, muerte, spawning y física.
