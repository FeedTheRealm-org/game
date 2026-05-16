using System;
using System.Threading;
using API;
using FTR.Core.Common.Scopes;
using FTR.Core.Server.Config;
using FTR.Core.Server.Healthcheck;
using FTR.Core.Server.Metrics;
using FTR.Core.Server.Persistence;
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
    private readonly OrchestratorService orchestratorService;
    private readonly Logging.Logger logger;

    private readonly CancellationTokenSource lifetimeCts = new();
    private bool disposed;

    private readonly float tickStep = 1f / 30f;
    private float accumulator;

    private bool IsInitialized = false;

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
        OrchestratorService orchestratorService,
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
        this.orchestratorService = orchestratorService;
        this.logger = logger;
        resolverContainer.SetResolver(resolver);

        if (serverConfig.LoadEnvVars)
        {
            secretsConfig.LoadEnvironmentVariables(
                serverConfig.EnvFilePath,
                serverConfig.LoadFromEnvFile
            );
        }

        serverConfig.LoadParams();

        Application.quitting += OnApplicationQuitting;
    }

    public async void Start()
    {
        try
        {
            var loadSucceeded = await worldLoader.LoadWorld();
            if (!loadSucceeded)
                throw new Exception("World loading failed");

            await database.Connect(
                secretsConfig.MongoConnectionString,
                serverConfig.WorldId,
                lifetimeCts.Token
            );
            await playersRepository.Connect(database);

            if (lifetimeCts.IsCancellationRequested)
                return;

            DogStatsd.Configure(
                secretsConfig.DDAgentHost,
                8125,
                new[] { $"world_id:{serverConfig.WorldId}", $"zone_id:{serverConfig.ZoneId}" }
            );

            healthcheckServer.Start();
            WorldLoadBootstrap.MarkServerReady();
            IsInitialized = true;
            await orchestratorService.UpdateZoneStatus(
                serverConfig.WorldId,
                serverConfig.ZoneId,
                isOnline: true
            );
            logger.Log(
                $"ServerWorldEntryPoint started successfully with worldId={serverConfig.WorldId}, zoneId={serverConfig.ZoneId}, isTestWorld={serverConfig.IsTestWorld}"
            );
        }
        catch (OrchestratorServiceException ex)
        {
            logger.Log($"Failed to update zone status: {ex.Message}", Logging.LogType.Error);
            WorldLoadBootstrap.MarkServerFailed();
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

        OnApplicationQuitting();
        lifetimeCts.Cancel();
        lifetimeCts.Dispose();
    }

    private async void OnApplicationQuitting()
    {
        if (disposed)
            return;
        disposed = true;

        logger.Log("Application quitting - notifying orchestrator of offline status");
        try
        {
            await orchestratorService.UpdateZoneStatus(
                serverConfig.WorldId,
                serverConfig.ZoneId,
                isOnline: false
            );
            await healthcheckServer.CloseAsync();
        }
        catch (OrchestratorServiceException ex)
        {
            logger.Log(
                $"Failed to update zone status on shutdown: {ex.Message}",
                Logging.LogType.Error
            );
        }
        catch (Exception ex)
        {
            logger.Log($"Error during application quit: {ex}", Logging.LogType.Error);
        }
    }

    /// <summary>
    /// Tick method is called by the VContainer's TickableManager every frame
    /// (60 TPS or as stated in server entrypoint), and it will call ServerTickDriver and NetworkTickDriver.
    /// </summary>
    public void Tick()
    {
        if (!IsInitialized)
            return;

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
