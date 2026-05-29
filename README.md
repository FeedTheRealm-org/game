# Feed the Realm — Game

Main repository for the Feed the Realm game. Contains the client and headless server implementation for the playable executable.

Built with **Unity 6000.3.10f1**.

To visit the World Editor repository [[click here]](https://github.com/FeedTheRealm-org/world-editor).

## How to build

Open the project in Unity (version `6000.3.10f1`) and build the client and server executables from `File > Build Profiles`.

## How to run client

After building, run the generated executable from the `Builds/Client/` folder.

## How to run server

After building, the server executable will be in `Builds/Server/`. It can be run standalone or via Docker.

### Secrets

Set the following as environment variables or in a `.env` file:

| Variable | Description |
|---|---|
| `MONGO_CONNECTION_STRING` | MongoDB connection string (local: `mongodb://admin:admin@localhost:27017/?authSource=admin` or cloud instance if IP access is granted) |
| `SERVER_FIXED_TOKEN` | Server access token — use a mock value for local development or the real one when connecting to the cloud core-service |
| `DD_AGENT_HOST` | Datadog agent host for metrics/tracing |
| `WORLD_ID` | World identifier — only used in Docker Compose |
| `ZONE_ID` | Zone identifier — only used in Docker Compose |
| `IS_TEST_WORLD` | Marks the server as a test world — only used in Docker Compose |
| `PORT` | UDP/TCP game port (default: `7777`) — only used in Docker Compose |
| `HPORT` | Health check port (default: `6001`) — only used in Docker Compose |

```bash
cp .env.example .env
```

## CI/CD

Workflow definitions are located in `.github/workflows/`:

| Workflow | Description |
|---|---|
| `precommit-check.yml` | Linting and formatting checks on pull requests |
| `build.yml` | Compiles the project and verifies the build succeeds |
| `ci-cd.yml` | Combined pipeline for building and deploying |
| `deploy.yml` | Deploys the production-ready server image |
| `git-leaks.yml` | Scans for accidentally committed secrets |

## Makefile Commands

Run `make help` to list all available commands.

```bash
# Development
make dev          # Start MongoDB + run the DEBUG server executable via entrypoint
make up           # Start all containers (production-like)
make up-build     # Build & start all containers (production-like)
make up-db        # Start only MongoDB
make build        # Build all containers (no start)
make down         # Stop and remove all containers
make logs         # Tail logs from all containers
make logs-<svc>   # Tail logs from a specific service (e.g. make logs-mongo)
make db           # Open a mongosh shell in the MongoDB container
make clean        # Remove all containers, images, and volumes
```

## Structure

```
.
├── Assets/
│   ├── 1_FeedTheRealm/       # Project source (scenes, scripts, prefabs)
│   ├── 6000FantasyIcons/     # Asset pack
│   ├── Cartoon_Texture_Pack/ # Asset pack
│   ├── GUI_Parts/            # Asset pack
│   ├── HeroEditor4D/         # Asset pack
│   ├── Mirror/               # Networking library
│   ├── Plugins/              # Third-party plugins
│   ├── SPUM/                 # Asset pack
│   ├── Settings/             # Unity project settings assets
│   ├── SyntyStudios/         # Asset pack
│   └── TextMesh Pro/         # Asset pack
├── Builds/                   # Compiled client and server executables
├── Docs/                     # Project documentation
├── Packages/                 # Unity package manifest
├── ProjectSettings/          # Unity project settings
├── Scripts/                  # Shell scripts
├── .github/workflows/        # CI/CD pipeline definitions
├── docker-compose.yml
├── Dockerfile
├── entrypoint.sh
├── entrypoint.dev.sh
└── Makefile
```

## Asset Packs

❗ **Please keep this list up to date!**

| Pack | Link |
|---|---|
| 6000 Fantasy Icons | [6000 Fantasy Icons](https://assetstore.unity.com/packages/2d/gui/icons/6000-fantasy-icons-278148) |
| Cartoon Texture Pack | [Stylized PBR Textures Pack](https://assetstore.unity.com/packages/2d/textures-materials/free-stylized-pbr-textures-pack-111778) |
| GUI Parts | [GUI Parts](https://assetstore.unity.com/packages/2d/gui/icons/gui-parts-159068) |
| HeroEditor4D | [Character Editor 4D [Megapack]](https://assetstore.unity.com/packages/2d/characters/character-editor-4d-megapack-147364) |
| SPUM | [2D Pixel Unit Maker - SPUM](https://assetstore.unity.com/packages/2d/characters/2d-pixel-unit-maker-spum-188715) |
| TextMesh Pro | [TextMesh Pro](https://docs.unity3d.com/Manual/com.unity.textmeshpro.html) |

## External Dependencies

❗ **Please keep this list up to date!**

| Package | Link |
|---|---|
| Feed the Realm — Shared Package | [shared-unity-package](https://github.com/feedTheRealm-org/shared-unity-package/) |
| Mirror | [mirror-networking.com](https://mirror-networking.com/) |
| VContainer | [vcontainer.hadashikick.jp](https://vcontainer.hadashikick.jp/) |
| UniTask | [Cysharp/UniTask](https://github.com/Cysharp/UniTask) |
| ParrelSync | [VeriorPies/ParrelSync](https://github.com/VeriorPies/ParrelSync) |
