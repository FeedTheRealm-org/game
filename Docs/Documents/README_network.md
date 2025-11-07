# Documentación de Scripts - Feed The Realm

Este documento describe los scripts principales utilizados en el proyecto Feed The Realm, un juego multiplayer desarrollado con Unity y Unity Netcode. Cada sección explica qué hace el script, sus dependencias, configuraciones necesarias y campos serializables.

## Resumen General

### Servidor Headless
- **Funcionamiento**: El servidor es headless (sin renderizado gráfico), ejecutándose en builds dedicados con el define `UNITY_SERVER`. No renderiza gráficos ni maneja input del usuario, solo procesa lógica de red y simulación del juego.
- **Diferencia con Cliente**: Los builds de cliente incluyen UI, renderizado y controles de usuario, mientras que el servidor solo ejecuta lógica de red. Ambos usan Unity Netcode, pero el servidor inicia automáticamente con `ServerBootstrap`.
- **Renderizado**: El servidor NO renderiza al cliente; solo procesa estado del juego y sincroniza datos.

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

**Descripción**: Gestiona la UI y lógica de la escena del juego, incluyendo desconexión.

**Campos Serializables**:
- `disconnectButton` (Button): Botón para desconectar.
- `returnToMenuPanel` (GameObject): Panel de retorno al menú.
- `logger` (Logging.Logger): Para logging.

**Configuraciones Esperadas**:
- Adjunto en la escena del juego.
- UI configurada con botones y paneles.

**Dependencias**:
- Unity.Netcode
- UnityEngine.UI
- UnityEngine.InputSystem
- Logging.Logger

## Notas Generales

- Todos los scripts usan un sistema de logging personalizado (`Logging.Logger`).
- La mayoría requieren Unity Netcode para funcionalidad de red.
- Campos serializables deben configurarse en el Inspector de Unity.
- Algunos scripts son singletons y persisten entre escenas.
- Verificar que las escenas referenciadas estén incluidas en Build Settings.