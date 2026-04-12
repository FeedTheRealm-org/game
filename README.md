# Feed the realm Game

This is the Main repository of the Feed the realm Game, it contains the client and headless server implementation
for the playable executable.

To visit the other World Editor executable [[Click Here]](https://github.com/feedTheRealm-org/world-editor-client/).

## How to build

No clear build instructions yet, but you can open the project in Unity and build the client and server executables from there.

## How to run client

To run the client after building the executable, you can simply run the generated executable in the `Build/Client/` folder.

## How to run server

Once you have built the server executable in the `Build/Server/` folder, you can run it using the following commands:

### Secrets

The secrets needed to be set as env var or to be specified in the `.env` file are:

- MONGO_CONNECTION_STRING (could be local or the real cloud instance if IP access is granted)
- FTR_SERVER_ACCESS_TOKEN (could be mock or real one -> depends if running core-service locally or connecting to cloud)
- WORLD_ID (only used in compose)
- ZONE_ID (only used in compose)

```bash
# for default envs
cp .env.example .env
```

### Standalone docker container

```bash
# Build the image (it wont build the server, only copy the already built executable to the image)
docker build -t ftr-server:latest .

# Run the server forwarding the ports 7777 for both TCP and UDP
docker run --rm -p 7777:7777/udp -p 7777:7777/tcp ftr-server:latest
```

### FTR Server container + local MongoDB

You can run the compose to be able to use a local mongo database from the local ftr-server container running.

```bash
# Build and run server + mongo
docker compose --profile all up --build

# Only run mongo db (useful for development and running server from unity  editor)
docker compose up
```

## CI/CD

No CI/CD yet.

## Assets packs

The following is a list of the assets packs used in this project.

❗ **Please keep this list up to date!**

- [6000 Fantasy Icons]()
- [Cartoon Texture Pack]()
- [GUI Parts]()
- [Polygon Arsenal]()
- [RPGPP_LT]()
- [Spum]()
- [HeroEditor4d]()
- [TextMeshPro]()

## External Dependencies (Non Unity Built-in)

The following is a list of the dependencies used in this project.

❗ **Please keep this list up to date!**

- [Feed the realm - Shared Package](http://github.com/feedTheRealm-org/shared-unity-package/)
- [Mirror](https://mirror-networking.com/)
- [VContainer](https://vcontainer.hadashikick.jp/)
- [UniTask](https://github.com/Cysharp/UniTask)
- [ParrelSync](https://github.com/VeriorPies/ParrelSync)
