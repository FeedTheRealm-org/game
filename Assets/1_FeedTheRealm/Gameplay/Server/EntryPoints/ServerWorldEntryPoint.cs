using System;
using System.Threading;
using FTR.Core.Common.Scopes;
using FTR.Core.Server.Config;
using FTR.Core.Server.Healthcheck;
using FTR.Core.Server.Persistance;
using FTR.Gameplay.Server.Loaders;
using FTR.Gameplay.Server.Scopes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class ServerWorldEntryPoint : IStartable, ITickable, IDisposable
{
    private readonly ServerTickDriver serverTickDriver;
    private readonly NetworkTickDriver networkTickDriver;

    private readonly ServerWorldLoader worldLoader;
    private readonly HealthcheckServer healthcheckServer;

    private readonly ServerConfig serverConfig;
    private readonly ServerSecretsConfig secretsConfig;
    private readonly Database database;
    private readonly PlayersRepository playersRepository;

    private readonly Logging.Logger logger;

    private readonly CancellationTokenSource lifetimeCts = new();
    private bool disposed;

    private readonly float tickStep = 1f / 30f;
    private float accumulator;

    public ServerWorldEntryPoint(
        ServerTickDriver serverTickDriver,
        NetworkTickDriver networkTickDriver,
        IObjectResolver resolver,
        ObjectResolverContainer resolverContainer,
        ServerWorldLoader worldLoader,
        HealthcheckServer healthcheckServer,
        ServerConfig serverConfig,
        ServerSecretsConfig secretsConfig,
        Database database,
        PlayersRepository playersRepository,
        Logging.Logger logger
    )
    {
        this.serverTickDriver = serverTickDriver;
        this.networkTickDriver = networkTickDriver;
        this.worldLoader = worldLoader;
        this.healthcheckServer = healthcheckServer;
        this.serverConfig = serverConfig;
        this.secretsConfig = secretsConfig;
        this.database = database;
        this.playersRepository = playersRepository;
        this.logger = logger;
        resolverContainer.SetResolver(resolver);

        secretsConfig.LoadEnvironmentVariables(
            serverConfig.EnvFilePath,
            serverConfig.LoadFromEnvFile
        );
    }

    public async void Start()
    {
        try
        {
            var loadSucceeded = await worldLoader.LoadWorld();
            if (!loadSucceeded)
                throw new Exception("World loading failed");

            string worldId = "world1";
            string zoneId = "1";
            logger.Log(
                $"Connecting to database with connection string: {secretsConfig.MongoConnectionString}"
            );
            await database.Connect(
                secretsConfig.MongoConnectionString,
                worldId,
                zoneId,
                lifetimeCts.Token
            );
            logger.Log("Database connected successfully");
            await playersRepository.Connect(database);
            logger.Log("PlayersRepository connected successfully");

            if (lifetimeCts.IsCancellationRequested)
                return;

            healthcheckServer.Start();
            WorldLoadBootstrap.MarkServerReady();
        }
        catch (OperationCanceledException)
        {
            logger.Log("ServerWorldEntryPoint startup cancelled.", Logging.LogType.Warning);
            WorldLoadBootstrap.MarkServerFailed();
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to start ServerWorldEntryPoint: {ex}", Logging.LogType.Error);
            WorldLoadBootstrap.MarkServerFailed();
        }
    }

    public async void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        lifetimeCts.Cancel();
        WorldLoadBootstrap.MarkServerFailed();

        try
        {
            await healthcheckServer.CloseAsync();
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to close HealthcheckServer: {ex}", Logging.LogType.Error);
        }

        lifetimeCts.Dispose();
    }

    /// <summary>
    /// Tick method is called by the VContainer's TickableManager every frame
    /// (60 TPS or as stated in server entrypoint), and it will call ServerTickDriver and NetworkTickDriver.
    /// </summary>
    public void Tick()
    {
        networkTickDriver.TickBefore();

        accumulator += Time.deltaTime;

        if (accumulator >= tickStep)
        {
            serverTickDriver.TickOnce(tickStep);
            accumulator -= tickStep;

            // Prevent spiral of death by ticking once per tickStep and not catching up
            if (accumulator > tickStep)
                accumulator = tickStep;
        }

        networkTickDriver.TickAfter();
    }
}
