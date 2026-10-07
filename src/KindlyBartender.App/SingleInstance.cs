using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using KindlyBartender.Core.Diagnostics;

namespace KindlyBartender.App;

/// <summary>
/// Keeps one copy of the app per user. A second copy asks the first, through a pipe only the same user can
/// open, to show itself, and then exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string ShowMessage = "show";

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _stop = new();
    private readonly bool _owned;
    private Task? _listener;

    public SingleInstance(string appName)
    {
        // The SID keeps the names apart for users sharing a PC through fast user switching.
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? "unknown";
        _mutex = new Mutex(initiallyOwned: true, $@"Local\{appName}-{sid}", out _owned);
        _pipeName = $"{appName}-{sid}";
    }

    /// <summary>Whether this is the first copy.</summary>
    public bool IsFirst => _owned;

    /// <summary>Asks the first copy to show itself. Returns false if it did not answer in time.</summary>
    public bool SignalFirst()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(TimeSpan.FromSeconds(3));
            using var writer = new StreamWriter(client);
            writer.Write(ShowMessage);
            return true;
        }
        catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Listens for later copies; <paramref name="onShow"/> runs on a background thread.</summary>
    public void Listen(Action onShow, Action<LogEvent, Exception> onError) =>
        _listener = Task.Run(async () =>
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        _pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                    // A client that connects and never sends must not block later copies.
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(2));
                    var buffer = new byte[ShowMessage.Length];
                    var read = await server.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, timeout.Token).ConfigureAwait(false);
                    if (System.Text.Encoding.UTF8.GetString(buffer, 0, read) == ShowMessage)
                    {
                        onShow();
                    }
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    // Anything else is logged and the listener keeps going, so a later copy can still reach it.
                    onError(LogEvent.ListenForOtherCopyFailed, e);
                    // Avoid a tight loop if the pipe keeps failing.
                    await Task.Delay(TimeSpan.FromSeconds(5), _stop.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            }
        });

    public void Dispose()
    {
        _stop.Cancel();
        // Wait briefly so the listener never uses the token source after it is disposed.
        if (_listener?.Wait(TimeSpan.FromSeconds(1)) != false)
        {
            _stop.Dispose();
        }

        if (_owned)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
