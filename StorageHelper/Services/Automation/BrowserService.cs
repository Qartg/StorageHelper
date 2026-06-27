using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace StorageHelper.Services.Automation
{
    public interface IBrowserSession
    {
        IPage Page { get; }
        event Action? Disconnected;
        Task<bool> EnsureStartedAsync(string warmupLink, CancellationToken ct = default);
    }

    public class BrowserSession : IBrowserSession, IAsyncDisposable
    {
        private readonly string ProfilePath;
        private readonly ILogger<BrowserSession> _logger;
        private IPlaywright? _pw;
        private IBrowser? _browser;
        private IPage? _page;
        private bool _ready;

        public IPage Page => _page ?? throw new InvalidOperationException("Страница не загружена");
        public event Action? Disconnected;

        public BrowserSession(string profilePath, ILogger<BrowserSession> logger)
        {
            ProfilePath = profilePath;
            _logger = logger;
        }

        public async Task<bool> EnsureStartedAsync(string warmupLink, CancellationToken ct = default)
        {
            try
            {
                if (_ready && _page != null) return true;
                _pw ??= await Playwright.CreateAsync();

                if (await WaitForCDPAsync(ct, 2.5) == null)
                    await TryStartBrowser(warmupLink);

                if (await WaitForCDPAsync(ct) is int port)
                {
                    _browser ??= await _pw.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}");
                    var ctx = _browser.Contexts.Count > 0 ? _browser.Contexts[0] : await _browser.NewContextAsync();
                    _page = ctx.Pages.Count > 0 ? ctx.Pages[0] : await ctx.NewPageAsync();
                    _browser.Disconnected += BrowserDisconnected;
                    ctx.SetDefaultTimeout(10000f);

                    _ready = true;

                    return true;
                }
                else
                    _logger.LogError("CDP не поднялся в течение 15 секунд");

                _ready = false;
                return false;
            }
            catch (Exception ex)
            {
                _ready = false;
                _logger.LogError(ex, "Не удалось запустить сессию автоматизации");
                return false;
            }
        }

        private void BrowserDisconnected(object? sender, IBrowser e)
        {
            Disconnected?.Invoke();

            _ready = false;
            _page = null;
            _browser = null;
        }

        private async Task<int?> ReadPortFromFile()
        {
            string filePath = Path.Combine(ProfilePath, "DevToolsActivePort");
            if (File.Exists(filePath))
            {
                using (var fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sReader = new StreamReader(fStream))
                {
                    string? line;

                    if ((line = await sReader.ReadLineAsync()) != null)
                        if (int.TryParse(line, out var port))
                            return port;
                }
            }
            return null;
        }

        private async Task TryStartBrowser(string warmupLink)
        {
            string? browserPath = FindBrowserPath();

            if (browserPath == null)
                throw new Exception("Не удалось найти поддерживаемый браузер в системе. Для работы автоматизации нужен Chrome или Edge");

            ProcessStartInfo psi = new()
            {
                FileName = browserPath,
                UseShellExecute = false,
            };

            psi.ArgumentList.Add($"--user-data-dir={ProfilePath}");
            psi.ArgumentList.Add("--no-first-run");
            psi.ArgumentList.Add("--no-default-browser-check");
            psi.ArgumentList.Add("--disable-session-crashed-bubble");

            if (!Directory.Exists(ProfilePath))
            {
                psi.ArgumentList.Add(warmupLink);
                var warmup = Process.Start(psi);
                if (warmup == null)
                    throw new Exception("Не удалось запустить процесс браузера");

                await WaitForCookies();
                warmup.Kill(true);
                await warmup.WaitForExitAsync();
            }

            psi.ArgumentList.Add("--remote-debugging-port=0");

            if (Process.Start(psi) == null)
                throw new Exception("Не удалось запустить процесс браузера");
        }

        private async Task<int?> WaitForCDPAsync(CancellationToken ct = default, double waitTime = 15)
        {
            using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(waitTime);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(200, ct);
                try
                {
                    ct.ThrowIfCancellationRequested();

                    int? port = await ReadPortFromFile();
                    if (port == null)
                        continue;

                    var response = await http.GetAsync($"http://127.0.0.1:{port}/json/version", ct);
                    if (response.IsSuccessStatusCode)
                        return port;
                }
                catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
                {
                    if (ct.IsCancellationRequested) return null;
                }
            }

            return null;
        }

        private async Task<bool> WaitForCookies(int timeoutSeconds = 35, CancellationToken ct = default)
        {
            string networkDir = Path.Combine(ProfilePath, "Default", "Network");
            string cookiesPath = Path.Combine(networkDir, "Cookies");
            string walPath = cookiesPath + "-wal";
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);

            while (!File.Exists(cookiesPath))
            {
                if (DateTime.UtcNow >= deadline || ct.IsCancellationRequested)
                    return false;
                await Task.Delay(200, ct);
            }

            using var fsw = new FileSystemWatcher(networkDir, "Cookies*")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            var lastChange = DateTime.UtcNow;
            fsw.Changed += (_, __) => lastChange = DateTime.UtcNow;

            var quietWindow = TimeSpan.FromMilliseconds(1500);
            while (DateTime.UtcNow - lastChange < quietWindow)
            {
                if (DateTime.UtcNow >= deadline || ct.IsCancellationRequested)
                    return false;
                await Task.Delay(200, ct);
            }

            long baseline = TotalLen(cookiesPath, walPath);

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            fsw.Changed += (_, __) =>
            {
                if (TotalLen(cookiesPath, walPath) > baseline)
                    tcs.TrySetResult();
            };

            if (TotalLen(cookiesPath, walPath) > baseline)
                tcs.TrySetResult();

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return false;

            try
            {
                await tcs.Task.WaitAsync(remaining, ct);
                return true;
            }
            catch (TimeoutException)
            {
                if (TotalLen(cookiesPath, walPath) > baseline)
                    return true;

                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private static long SafeLen(string p)
        {
            try { return new FileInfo(p).Length; }
            catch { return 0; }
        }
        private static long TotalLen(string cookiesPath, string walPath) => SafeLen(cookiesPath) + SafeLen(walPath);

        private static string? FindBrowserPath()
        {
            string?[] candidates = new string?[]
            {
                ReadAppPath("chrome.exe"), ReadAppPath("msedge.exe"),
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            };

            return candidates.FirstOrDefault(p => p is not null && File.Exists(p));
        }

        private static string? ReadAppPath(string exeName)
        {
            foreach (var root in new[] { "HKEY_LOCAL_MACHINE", "HKEY_CURRENT_USER" })
            {
                if (Registry.GetValue(@$"{root}\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}", null, null) is string p
                    && !string.IsNullOrEmpty(p))
                    return p.Trim('"');
            }
            return null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.DisposeAsync();
            if (_pw != null) _pw.Dispose();
        }
    }
}
