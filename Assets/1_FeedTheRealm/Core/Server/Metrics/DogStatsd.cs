using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FTR.Core.Server.Metrics;

public static class DogStatsd
{
    private static UdpClient _udp;
    private static IPEndPoint _endpoint;
    private static string _constantTags;
    private static bool configured = false;

    public static void Configure(string host, int port, string[] tags)
    {
        _udp = new UdpClient();

        if (IPAddress.TryParse(host, out var ip))
        {
            _endpoint = new IPEndPoint(ip, port);
        }
        else
        {
            var resolved_host_ips = Dns.GetHostEntry(host).AddressList;
            if (resolved_host_ips.Length == 0)
                throw new Exception($"Could not resolve host: {host}");

            _endpoint = new IPEndPoint(resolved_host_ips[0], port);
        }

        _constantTags = tags != null ? string.Join(",", tags) : "";
        configured = true;
    }

    private static void Send(string metric)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(metric);
            _udp.Send(bytes, bytes.Length, _endpoint);
        }
        catch { }
    }

    private static string Tags(string[] extra)
    {
        if (extra != null && extra.Length > 0)
        {
            var all =
                _constantTags.Length > 0
                    ? _constantTags + "," + string.Join(",", extra)
                    : string.Join(",", extra);
            return $"|#{all}";
        }
        return _constantTags.Length > 0 ? $"|#{_constantTags}" : "";
    }

    public static void Gauge(string name, double value, string[] tags = null)
    {
        if (!configured)
            return;
        Send($"{name}:{value}|g{Tags(tags)}");
    }

    public static void Histogram(string name, double value, string[] tags = null)
    {
        if (!configured)
            return;
        Send($"{name}:{value}|h{Tags(tags)}");
    }

    public static void Increment(string name, int value = 1, string[] tags = null)
    {
        if (!configured)
            return;
        Send($"{name}:{value}|c{Tags(tags)}");
    }
}
