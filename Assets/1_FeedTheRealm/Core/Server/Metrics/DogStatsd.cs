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

    public static void Configure(string host, int port, string[] tags)
    {
        _udp = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        _constantTags = tags != null ? string.Join(",", tags) : "";
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

    public static void Gauge(string name, double value, string[] tags = null) =>
        Send($"{name}:{value}|g{Tags(tags)}");

    public static void Histogram(string name, double value, string[] tags = null) =>
        Send($"{name}:{value}|h{Tags(tags)}");

    public static void Increment(string name, int value = 1, string[] tags = null) =>
        Send($"{name}:{value}|c{Tags(tags)}");
}
