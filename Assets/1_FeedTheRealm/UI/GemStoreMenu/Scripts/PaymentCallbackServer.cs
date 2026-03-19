using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public struct PaymentData
{
    public string PackName;
    public int GemAmount;
    public string Price;
    public int NewBalance;
}

public class PaymentCallbackServer : MonoBehaviour
{
    public const int Port = 9876;
    public const string SuccessPath = "/payment/success/";
    public const string CancelPath = "/payment/cancel/";

    public string SuccessUrl => $"http://localhost:{Port}{SuccessPath}";
    public string CancelUrl => $"http://localhost:{Port}{CancelPath}";

    public event Action OnSuccessEvent;
    public event Action OnCancelledEvent;

    private HttpListener listener;
    private CancellationTokenSource cts;

    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
    private readonly object queueLock = new object();

    public void StartServer(PaymentData data)
    {
        StopServer();

        cts = new CancellationTokenSource();
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{Port}/payment/");
        listener.Start();

        Debug.Log($"[PaymentCallbackServer] Listening on port {Port}");
        Task.Run(() => ListenAsync(cts.Token, data), cts.Token);
    }

    public void StopServer()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (listener != null && listener.IsListening)
            listener.Stop();
        listener = null;

        Debug.Log("[PaymentCallbackServer] Stopped.");
    }

    private void Update()
    {
        lock (queueLock)
        {
            while (mainThreadQueue.Count > 0)
                mainThreadQueue.Dequeue().Invoke();
        }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private async Task ListenAsync(CancellationToken token, PaymentData data)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                string path = ctx.Request.Url.AbsolutePath;
                bool isSuccess = path.StartsWith(SuccessPath);

                string html = await GetHTMLAsync(isSuccess);
                html = ReplaceTemplate(html, data);

                await RespondAsync(ctx, html, token);

                Enqueue(() =>
                {
                    if (isSuccess)
                        OnSuccessEvent?.Invoke();
                    else
                        OnCancelledEvent?.Invoke();
                });

                StopServer();
                break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.LogError($"[PaymentCallbackServer] Error: {ex.Message}");
        }
    }

    private void Enqueue(Action action)
    {
        lock (queueLock)
            mainThreadQueue.Enqueue(action);
    }

    private static async Task RespondAsync(
        HttpListenerContext ctx,
        string html,
        CancellationToken token
    )
    {
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        ctx.Response.ContentType = "text/html";
        ctx.Response.ContentLength64 = buffer.Length;
        await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, token);
        ctx.Response.OutputStream.Close();
    }

    private static string ReplaceTemplate(string html, PaymentData data)
    {
        return html.Replace("{{.PackName}}", data.PackName)
            .Replace("{{.GemAmount}}", data.GemAmount.ToString())
            .Replace("{{.Price}}", data.Price)
            .Replace("{{.NewBalance}}", data.NewBalance.ToString());
    }

    private static async Task<string> GetHTMLAsync(bool isSuccess)
    {
        string path;
        if (isSuccess)
            path = Path.Combine(
                Application.streamingAssetsPath,
                "Templates",
                "PaymentSuccess.html"
            );
        else
            path = Path.Combine(Application.streamingAssetsPath, "Templates", "PaymentCancel.html");
        string html = await File.ReadAllTextAsync(path);
        return html;
    }
}
