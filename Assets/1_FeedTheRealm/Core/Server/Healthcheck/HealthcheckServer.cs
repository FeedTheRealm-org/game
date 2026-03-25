using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FTR.Core.Server.Healthcheck;

public class HealthcheckServer
{
    private readonly ushort _port;
    private TcpListener _listener;
    private CancellationTokenSource _cts;
    private Task _listenerTask;

    public HealthcheckServer(FTR.Core.Common.Config.Config config)
    {
        _port = config.HealthcheckPort;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        Debug.Log($"Healthcheck server started on port {_port}");

        _listenerTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    client.Close();
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        });
    }

    public async Task CloseAsync()
    {
        Debug.Log("Shutting down healthcheck server...");
        _cts?.Cancel();
        _listener?.Stop();

        if (_listenerTask != null)
            await _listenerTask;
    }
}
