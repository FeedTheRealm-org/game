using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using API;
using FTR.Core.Server.Config;
using VContainer;

namespace FTR.Core.Server.Metrics;

public class PlayerStatsSender : IDisposable
{
    private readonly ServerConfig serverConfig;
    private readonly WorldMonitor worldMonitor;
    private readonly ZoneService zoneService;

    private Dictionary<uint, DateTime> playerConnectionTimes = new();
    private int currentPlayerTimeAvgMins = 0;
    private int totalPlayersRecorded = 0;

    public PlayerStatsSender(
        ServerConfig serverConfig,
        WorldMonitor worldMonitor,
        ZoneService zoneService
    )
    {
        this.serverConfig = serverConfig;
        this.worldMonitor = worldMonitor;
        this.zoneService = zoneService;

        worldMonitor.Entities.OnPlayerCountChanged += HandlePlayerCountChanged;
    }

    public void Dispose()
    {
        worldMonitor.Entities.OnPlayerCountChanged -= HandlePlayerCountChanged;
    }

    public void Send()
    {
        _ = zoneService.UpdatePlayerCount(
            serverConfig.WorldId,
            serverConfig.ZoneId,
            worldMonitor.Entities.PlayerCount,
            currentPlayerTimeAvgMins
        );
    }

    private void HandlePlayerCountChanged(uint netId, bool newConnection)
    {
        if (newConnection)
        {
            playerConnectionTimes[netId] = DateTime.UtcNow;
        }
        else
        {
            // Calculate rolling average for player time
            if (playerConnectionTimes.TryGetValue(netId, out var connectionTime))
            {
                var sessionDuration = DateTime.UtcNow - connectionTime;
                var sessionDurationMins = (int)sessionDuration.TotalMinutes;

                totalPlayersRecorded++;
                currentPlayerTimeAvgMins =
                    ((currentPlayerTimeAvgMins * (totalPlayersRecorded - 1)) + sessionDurationMins)
                    / totalPlayersRecorded;

                playerConnectionTimes.Remove(netId);
            }
        }
    }
}
