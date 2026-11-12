using System.Security.Cryptography;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace Ssc;

public sealed class SteamSession : IAsyncDisposable
{
    private const int MaxReconnects = 30;

    private static readonly HashSet<EResult> ExpiredLoginResults =
    [
        EResult.InvalidPassword,
        EResult.AccessDenied,
        EResult.Expired,
        EResult.AccountLoginDeniedNeedTwoFactor,
        EResult.InvalidSignature,
    ];

    private readonly TokenStore tokens;
    private readonly QrDisplay qr;
    private readonly TextWriter log;
    private readonly CancellationToken ct;
    private readonly CallbackManager callbacks;
    private readonly SteamUser user;
    private readonly TaskCompletionSource loggedOn = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread callbackThread;
    private SavedLogin? login;
    private int reconnects;
    private volatile bool stopping;

    public SteamClient Client { get; } = new();
    public SteamUnifiedMessages Unified { get; }
    public Cloud Cloud { get; }
    public SteamApps Apps { get; }
    public SteamID SteamId { get; private set; } = new();
    public string AccountName => login?.AccountName ?? "";

    private SteamSession(TokenStore tokens, QrDisplay qr, TextWriter log, CancellationToken ct)
    {
        this.tokens = tokens;
        this.qr = qr;
        this.log = log;
        this.ct = ct;
        login = tokens.Load();
        callbacks = new CallbackManager(Client);
        user = Client.GetHandler<SteamUser>()!;
        Apps = Client.GetHandler<SteamApps>()!;
        Unified = Client.GetHandler<SteamUnifiedMessages>()!;
        Cloud = Unified.CreateService<Cloud>();
        callbacks.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        callbacks.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        callbacks.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        callbackThread = new Thread(RunCallbacks) { IsBackground = true, Name = "steam-callbacks" };
    }

    public static async Task<SteamSession> ConnectAsync(TokenStore tokens, QrDisplay qr, TextWriter log, CancellationToken ct)
    {
        var session = new SteamSession(tokens, qr, log, ct);
        try
        {
            using var cancel = ct.Register(() => session.loggedOn.TrySetCanceled(ct));
            session.callbackThread.Start();
            log.WriteLine("Connecting to Steam...");
            session.Client.Connect();
            await session.loggedOn.Task;
            return session;
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    public async Task<string> GetWebAccessTokenAsync()
    {
        var result = await Client.Authentication.GenerateAccessTokenForAppAsync(SteamId, login!.RefreshToken, allowRenewal: false);
        return result.AccessToken;
    }

    public async ValueTask DisposeAsync()
    {
        if (loggedOn.Task.IsCompletedSuccessfully && Client.IsConnected)
        {
            user.LogOff();
            await Task.WhenAny(disconnected.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        }
        stopping = true;
        Client.Disconnect();
        if (callbackThread.IsAlive)
        {
            callbackThread.Join(TimeSpan.FromSeconds(1));
        }
    }

    private void RunCallbacks()
    {
        while (!stopping)
        {
            callbacks.RunWaitCallbacks(TimeSpan.FromMilliseconds(250));
        }
    }

    private void OnConnected(SteamClient.ConnectedCallback callback)
    {
        if (login is not null)
        {
            LogOn(login);
        }
        else
        {
            _ = LogOnWithQrAsync();
        }
    }

    private async Task LogOnWithQrAsync()
    {
        try
        {
            var auth = await Client.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails { DeviceFriendlyName = "ssc" });
            auth.ChallengeURLChanged = () => qr.Show(auth.ChallengeURL);
            qr.Show(auth.ChallengeURL);
            var result = await auth.PollingWaitForResultAsync(ct);
            login = new SavedLogin(result.AccountName, result.RefreshToken);
            tokens.Save(login);
            qr.Clear();
            LogOn(login);
        }
        catch (Exception e) when (e is AsyncJobFailedException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            // The CM dropped the connection while the QR was waiting; the reconnect starts a fresh QR session.
        }
        catch (AuthenticationException e)
        {
            log.WriteLine($"QR login failed ({e.Result}), starting a new one.");
            Client.Disconnect();
        }
        catch (Exception e)
        {
            loggedOn.TrySetException(e);
        }
    }

    private void LogOn(SavedLogin saved) =>
        user.LogOn(new SteamUser.LogOnDetails
        {
            Username = saved.AccountName,
            AccessToken = saved.RefreshToken,
            ShouldRememberPassword = true,
            // Without a unique LoginID the logon collides with the desktop client's session from the same IP.
            LoginID = (uint)RandomNumberGenerator.GetInt32(1, int.MaxValue),
        });

    private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            SteamId = Client.SteamID!;
            loggedOn.TrySetResult();
        }
        else if (callback.Result is EResult.TryAnotherCM or EResult.ServiceUnavailable)
        {
            Client.Disconnect();
        }
        else if (ExpiredLoginResults.Contains(callback.Result) && login is not null)
        {
            tokens.Delete();
            login = null;
            log.WriteLine("Saved login expired, scan a new QR code.");
            Client.Disconnect();
        }
        else
        {
            loggedOn.TrySetException(new InvalidOperationException($"Steam logon failed: {callback.Result} / {callback.ExtendedResult}"));
        }
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        if (loggedOn.Task.IsCompleted || stopping)
        {
            disconnected.TrySetResult();
            return;
        }
        if (++reconnects > MaxReconnects)
        {
            loggedOn.TrySetException(new InvalidOperationException($"Could not reach a Steam server after {MaxReconnects} attempts."));
            return;
        }
        _ = Task.Delay(TimeSpan.FromSeconds(Math.Min(reconnects, 5)), ct).ContinueWith(delay =>
        {
            if (!delay.IsCanceled && !stopping)
            {
                Client.Connect();
            }
        }, TaskScheduler.Default);
    }
}
