using System.Net.Http.Headers;

using CardPicker2.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class ThemeBrowserFixture : IAsyncLifetime
{
    public static readonly IReadOnlyList<ThemeBrowserEngine> Engines =
    [
        new("chromium", "Chromium", SupportsAutomation: true),
        new("firefox", "Firefox", SupportsAutomation: true),
        new("webkit", "WebKit", SupportsAutomation: true)
    ];

    public static readonly IReadOnlyList<string> ManualBrowserCoverage =
    [
        "Safari 手動驗證 WebKit 自動化未涵蓋的瀏覽器品牌差異。",
        "Edge 手動驗證 Chromium 自動化未涵蓋的瀏覽器品牌差異。"
    ];

    public const string BaseUrl = "http://cardpicker.test";

    private readonly Dictionary<string, IBrowser> _browsers = [];
    private readonly List<IBrowserContext> _contexts = [];

    private TempCardLibrary? _library;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _serverClient;

    public IPlaywright Playwright { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _library = TempCardLibrary.Create("cardpicker-theme-browser-tests-");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    _library.Configure(services);
                });
            });
        _serverClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost")
        });
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }

        foreach (var browser in _browsers.Values)
        {
            await browser.DisposeAsync();
        }

        Playwright.Dispose();
        _serverClient?.Dispose();
        await (_factory?.DisposeAsync() ?? ValueTask.CompletedTask);
        _library?.Dispose();
    }

    public async Task<IPage> CreateDesktopPageAsync(string engineName = "chromium")
    {
        var context = await CreateContextAsync(engineName);
        return await context.NewPageAsync();
    }

    public async Task<IPage> CreateMobileTouchPageAsync(string engineName = "chromium")
    {
        var context = await CreateContextAsync(engineName, new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true
        });
        return await context.NewPageAsync();
    }

    public async Task<IBrowserContext> CreateContextAsync(
        string engineName = "chromium",
        BrowserNewContextOptions? options = null)
    {
        var browser = await GetBrowserAsync(engineName);
        var context = await browser.NewContextAsync(options ?? new BrowserNewContextOptions());
        await context.RouteAsync("**/*", RouteRequestToTestServerAsync);
        _contexts.Add(context);
        return context;
    }

    private async Task<IBrowser> GetBrowserAsync(string engineName)
    {
        if (_browsers.TryGetValue(engineName, out var existing))
        {
            return existing;
        }

        var browserType = engineName switch
        {
            "chromium" => Playwright.Chromium,
            "firefox" => Playwright.Firefox,
            "webkit" => Playwright.Webkit,
            _ => throw new ArgumentOutOfRangeException(
                nameof(engineName),
                engineName,
                "Browser engine must be chromium, firefox, or webkit.")
        };

        var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _browsers.Add(engineName, browser);
        return browser;
    }

    private async Task RouteRequestToTestServerAsync(IRoute route)
    {
        if (_serverClient is null)
        {
            throw new InvalidOperationException("The test server client has not been initialized.");
        }

        var requestUri = new Uri(route.Request.Url);
        if (!string.Equals(requestUri.Host, "cardpicker.test", StringComparison.OrdinalIgnoreCase))
        {
            await route.ContinueAsync();
            return;
        }

        using var request = new HttpRequestMessage(new HttpMethod(route.Request.Method), requestUri.PathAndQuery);
        foreach (var header in route.Request.Headers)
        {
            if (ShouldSkipRequestHeader(header.Key))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (route.Request.PostDataBuffer is { Length: > 0 } body)
        {
            request.Content = new ByteArrayContent(body);
            if (route.Request.Headers.TryGetValue("content-type", out var contentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }
        }

        using var response = await _serverClient.SendAsync(request);
        var responseHeaders = response.Headers
            .Concat(response.Content.Headers)
            .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => string.Join(",", group.SelectMany(header => header.Value)),
                StringComparer.OrdinalIgnoreCase);

        await route.FulfillAsync(new RouteFulfillOptions
        {
            Status = (int)response.StatusCode,
            Headers = responseHeaders,
            BodyBytes = await response.Content.ReadAsByteArrayAsync()
        });
    }

    private static bool ShouldSkipRequestHeader(string headerName)
    {
        return string.Equals(headerName, "host", StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, "content-length", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ThemeBrowserEngine(string Name, string DisplayName, bool SupportsAutomation);
