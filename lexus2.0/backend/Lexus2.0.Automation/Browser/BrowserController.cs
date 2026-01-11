using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Lexus2_0.Core.Logging;
using Lexus2_0.Core.Retry;
using Lexus2_0.Core.Proxy;
using Lexus2_0.Core.Cookies;

namespace Lexus2_0.Automation.Browser
{
    /// <summary>
    /// Browser automation controller using Playwright
    /// Handles browser lifecycle and page state observation
    /// </summary>
    public class BrowserController : IDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private readonly ILogger _logger;
        private readonly RetryPolicy _retryPolicy;
        private ProxyConfiguration? _proxyConfig;
        private CookieManager? _cookieManager;
        private bool _loginPostDetected = false;
        private TaskCompletionSource<bool>? _loginPostCompletionSource;

        public IPage? Page => _page;
        public bool IsInitialized => _page != null;
        public bool LoginPostDetected => _loginPostDetected;

        public BrowserController(ILogger logger)
        {
            _logger = logger;
            // Increased retries and delays for IRCTC (like NeXuS - handles slow networks better)
            _retryPolicy = new RetryPolicy(maxRetries: 5, initialDelay: TimeSpan.FromSeconds(3), maxDelay: TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Set proxy configuration
        /// </summary>
        public void SetProxy(ProxyConfiguration proxyConfig)
        {
            _proxyConfig = proxyConfig;
        }

        /// <summary>
        /// Set cookie manager for cookie handling
        /// </summary>
        public void SetCookieManager(CookieManager cookieManager)
        {
            _cookieManager = cookieManager;
        }

        /// <summary>
        /// Initialize browser with Playwright
        /// </summary>
        public async Task InitializeAsync(bool headless = false)
        {
            try
            {
                _logger.Info("Initializing browser...");
                
                _playwright = await Playwright.CreateAsync();
                
                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = headless,
                    Channel = "chrome", // Use installed Chrome
                    Args = new[] { "--disable-blink-features=AutomationControlled" } // Stealth mode
                };

                _browser = await _playwright.Chromium.LaunchAsync(launchOptions);

                // TeslaX uses specific viewport size (1478x1056) for better compatibility
                var contextOptions = new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1478, Height = 1056 }, // TeslaX viewport
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    // Additional stealth options (like TeslaX)
                    IgnoreHTTPSErrors = false,
                    JavaScriptEnabled = true,
                    Locale = "en-US",
                    TimezoneId = "Asia/Kolkata" // IRCTC is India-based
                };

                // Apply proxy if configured
                if (_proxyConfig != null && _proxyConfig.Enabled && _proxyConfig.IsValid())
                {
                    contextOptions.Proxy = new Proxy
                    {
                        Server = _proxyConfig.GetProxyServer()
                    };
                    _logger.Info($"Using proxy: {_proxyConfig.Host}:{_proxyConfig.Port}");
                }

                var context = await _browser.NewContextAsync(contextOptions);

                // TeslaX style: Clear cookies when proxy is applied (clearProxyAndCookies behavior)
                if (_proxyConfig != null && _proxyConfig.Enabled && _cookieManager != null)
                {
                    _logger.Info("Proxy enabled - clearing saved cookies for clean state (TeslaX clearProxyAndCookies style)");
                    _cookieManager.ClearCookies();
                    // Note: New context is already empty, no need to clear browser cookies
                }

                // Load cookies if available (only if proxy is not enabled - TeslaX style)
                if (_cookieManager != null && (_proxyConfig == null || !_proxyConfig.Enabled))
                {
                    var cookies = _cookieManager.LoadCookies();
                    if (cookies != null && _cookieManager.AreCookiesValid(cookies))
                    {
                        var playwrightCookies = ToPlaywrightCookies(cookies);
                        await context.AddCookiesAsync(playwrightCookies.ToArray());
                        _logger.Info($"Loaded {playwrightCookies.Count} cookies");
                    }
                }

                _page = await context.NewPageAsync();
                
                // Setup login POST detection (TeslaX style - detect /authprovider/webtoken)
                SetupLoginPostDetection();
                
                // Handle dialogs/alerts automatically (like Aadhaar booking window alert)
                _page.Dialog += async (sender, dialog) =>
                {
                    _logger.Info($"Dialog detected: {dialog.Type} - {dialog.Message}");
                    try
                    {
                        // Auto-accept dialogs (alerts, confirms, prompts)
                        await dialog.AcceptAsync();
                        _logger.Info("Dialog accepted automatically");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to accept dialog: {ex.Message}");
                    }
                };
                
                // Add enhanced stealth scripts (TeslaX style - hide automation)
                await _page.AddInitScriptAsync(@"
                    // Hide webdriver property
                    Object.defineProperty(navigator, 'webdriver', {
                        get: () => undefined
                    });
                    
                    // Override plugins (TeslaX style)
                    Object.defineProperty(navigator, 'plugins', {
                        get: () => [1, 2, 3, 4, 5]
                    });
                    
                    // Override languages
                    Object.defineProperty(navigator, 'languages', {
                        get: () => ['en-US', 'en']
                    });
                    
                    // Chrome runtime (TeslaX style)
                    window.chrome = {
                        runtime: {}
                    };
                    
                    // Permissions override
                    const originalQuery = window.navigator.permissions.query;
                    window.navigator.permissions.query = (parameters) => (
                        parameters.name === 'notifications' ?
                            Promise.resolve({ state: Notification.permission }) :
                            originalQuery(parameters)
                    );
                ");

                _logger.Info("Browser initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize browser", ex);
                throw;
            }
        }

        /// <summary>
        /// Navigate to URL with retry - improved for IRCTC (like NeXuS)
        /// </summary>
        public async Task NavigateAsync(string url, int timeout = 90000)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.Info($"Navigating to: {url}");
                
                try
                {
                    // First try with domcontentloaded (faster, less strict - like NeXuS)
                    await _page!.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = timeout
                    });
                    _logger.Info("Navigation completed (DOMContentLoaded)");
                    
                    // Wait a bit for page to stabilize
                    await Task.Delay(2000);
                    
                    // Try to wait for network idle, but don't fail if it times out
                    try
                    {
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                        {
                            Timeout = 30000
                        });
                        _logger.Debug("Network idle achieved");
                    }
                    catch (TimeoutException)
                    {
                        _logger.Warning("Network idle timeout, proceeding anyway (like NeXuS)");
                    }
                }
                catch (TimeoutException)
                {
                    _logger.Warning($"Navigation timeout, checking if page loaded anyway...");
                    
                    // Check if page actually loaded despite timeout
                    var currentUrl = _page!.Url;
                    if (currentUrl.Contains("irctc") || currentUrl.Contains("train-search"))
                    {
                        _logger.Info("Page appears to have loaded despite timeout, proceeding...");
                        await Task.Delay(2000);
                        return; // Page loaded, continue
                    }
                    
                    throw; // Re-throw if page didn't load
                }
            });
        }

        /// <summary>
        /// Wait for element with timeout
        /// </summary>
        public async Task<ILocator> WaitForElementAsync(string selector, int timeout = 10000, bool waitForVisible = true)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            try
            {
                var element = _page.Locator(selector).First;
                var waitOptions = new LocatorWaitForOptions 
                { 
                    Timeout = timeout,
                    State = waitForVisible ? WaitForSelectorState.Visible : WaitForSelectorState.Attached
                };
                await element.WaitForAsync(waitOptions);
                _logger.Debug($"Element found and ready: {selector}");
                return element;
            }
            catch (Exception ex)
            {
                _logger.Error($"Element not found: {selector}", ex);
                // Take screenshot for debugging
                try
                {
                    await _page!.ScreenshotAsync(new PageScreenshotOptions 
                    { 
                        Path = $"error-{DateTime.Now:yyyyMMddHHmmss}.png" 
                    });
                    _logger.Info($"Screenshot saved for debugging: error-{DateTime.Now:yyyyMMddHHmmss}.png");
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Click element with retry
        /// </summary>
        public async Task ClickAsync(string selector, bool force = false)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.Info($"Waiting for clickable element: {selector}");
                var element = await WaitForElementAsync(selector, timeout: 15000, waitForVisible: true);
                
                // Scroll element into view
                await element.ScrollIntoViewIfNeededAsync();
                await Task.Delay(300); // Small delay for stability
                
                _logger.Info($"Clicking element: {selector}");
                await element.ClickAsync(new LocatorClickOptions { Force = force });
                await Task.Delay(500); // Delay after click to allow page to react
                
                _logger.Debug($"Successfully clicked element: {selector}");
            });
        }

        /// <summary>
        /// Fill input field
        /// </summary>
        public async Task FillAsync(string selector, string value, bool clearFirst = true)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.Info($"Waiting for element: {selector}");
                var element = await WaitForElementAsync(selector, timeout: 15000, waitForVisible: true);
                
                // Scroll element into view
                await element.ScrollIntoViewIfNeededAsync();
                await Task.Delay(300); // Small delay for stability
                
                if (clearFirst)
                {
                    await element.ClearAsync();
                    await Task.Delay(200);
                }
                
                _logger.Info($"Filling element {selector} with value: {value.Substring(0, Math.Min(3, value.Length))}***");
                await element.FillAsync(value);
                await Task.Delay(300); // Delay after filling
                
                // Verify value was set
                var actualValue = await element.InputValueAsync();
                if (actualValue != value)
                {
                    _logger.Warning($"Value mismatch. Expected: {value}, Got: {actualValue}. Retrying...");
                    await element.ClearAsync();
                    await element.FillAsync(value);
                }
                
                _logger.Debug($"Successfully filled element {selector}");
            });
        }

        /// <summary>
        /// Select dropdown option
        /// </summary>
        public async Task SelectOptionAsync(string selector, string value)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var element = await WaitForElementAsync(selector);
                await element.SelectOptionAsync(value);
                _logger.Debug($"Selected option {value} in {selector}");
            });
        }

        /// <summary>
        /// Get text content
        /// </summary>
        public async Task<string> GetTextAsync(string selector)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            var element = await WaitForElementAsync(selector);
            return await element.TextContentAsync() ?? string.Empty;
        }

        /// <summary>
        /// Check if element exists
        /// </summary>
        public async Task<bool> ElementExistsAsync(string selector)
        {
            if (_page == null)
                return false;

            try
            {
                var element = _page.Locator(selector);
                var count = await element.CountAsync();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Handle alerts/dialogs on the page
        /// </summary>
        public async Task HandleAlertsAsync(int timeout = 5000)
        {
            if (_page == null)
                return;

            try
            {
                // Wait a bit for any dialogs to appear
                await Task.Delay(1000);
                
                // Check for common alert/dialog selectors
                var alertSelectors = new[]
                {
                    "button:has-text('OK')",
                    "button:has-text('Ok')",
                    "button:has-text('ok')",
                    ".ui-dialog-footer button",
                    ".modal-footer button",
                    "[role='dialog'] button",
                    ".alert button",
                    "button.btn-primary",
                    "button.btn"
                };

                foreach (var selector in alertSelectors)
                {
                    try
                    {
                        var button = _page.Locator(selector).First;
                        var count = await button.CountAsync();
                        if (count > 0)
                        {
                            var isVisible = await button.IsVisibleAsync();
                            if (isVisible)
                            {
                                var text = await button.TextContentAsync() ?? "";
                                if (text.Contains("OK", StringComparison.OrdinalIgnoreCase) || 
                                    text.Contains("Accept", StringComparison.OrdinalIgnoreCase) ||
                                    text.Contains("Agree", StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.Info($"Clicking alert/dialog button: {text}");
                                    await button.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                                    await Task.Delay(500);
                                    return;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue to next selector
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"No alerts found or error handling alerts: {ex.Message}");
            }
        }

        /// <summary>
        /// Wait for page state (network idle, DOM ready, etc.)
        /// </summary>
        public async Task WaitForPageStateAsync(LoadState state = LoadState.NetworkIdle)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            await _page.WaitForLoadStateAsync(state);
        }

        /// <summary>
        /// Take screenshot for debugging
        /// </summary>
        public async Task<string> TakeScreenshotAsync(string filename)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            var path = $"screenshots/{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            _logger.Info($"Screenshot saved: {path}");
            return path;
        }

        /// <summary>
        /// Execute JavaScript
        /// </summary>
        public async Task<T?> EvaluateAsync<T>(string script)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            return await _page.EvaluateAsync<T>(script);
        }

        /// <summary>
        /// Setup login POST detection (TeslaX style - detect /authprovider/webtoken endpoint)
        /// </summary>
        private void SetupLoginPostDetection()
        {
            if (_page == null) return;

            _loginPostDetected = false;
            _loginPostCompletionSource = new TaskCompletionSource<bool>();

            // Monitor responses for webtoken endpoint (TeslaX style)
            _page.Response += async (sender, response) =>
            {
                try
                {
                    var url = response.Url;
                    var method = response.Request.Method;
                    var status = response.Status;

                    // Detect login POST completion (TeslaX style)
                    if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                        status == 200 &&
                        url.Contains("/authprovider/webtoken"))
                    {
                        _logger.Info("[TeslaX Style] Detected IRCTC login POST completion");
                        _loginPostDetected = true;
                        
                        if (!_loginPostCompletionSource.Task.IsCompleted)
                        {
                            _loginPostCompletionSource.SetResult(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error in login POST detection: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// Wait for login POST completion (TeslaX style)
        /// </summary>
        public async Task WaitForLoginPostAsync(int timeoutMs = 60000)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            if (_loginPostDetected)
            {
                _logger.Debug("Login POST already detected");
                return;
            }

            if (_loginPostCompletionSource == null)
            {
                _logger.Warning("Login POST detection not set up");
                return;
            }

            _logger.Info("Waiting for login POST completion (TeslaX style)...");
            
            try
            {
                await Task.WhenAny(
                    _loginPostCompletionSource.Task,
                    Task.Delay(timeoutMs)
                );

                if (_loginPostDetected)
                {
                    _logger.Info("Login POST detected successfully");
                    await Task.Delay(1000); // Small delay after POST completion
                }
                else
                {
                    _logger.Warning($"Login POST detection timeout after {timeoutMs}ms");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error waiting for login POST: {ex.Message}");
            }
        }

        /// <summary>
        /// Wait for click threshold (TeslaX style - 2 clicks before cookie collection)
        /// </summary>
        public async Task WaitForClickThresholdAsync(int threshold = 2, int timeoutMs = 60000)
        {
            if (_page == null)
                throw new InvalidOperationException("Browser not initialized");

            _logger.Info($"Waiting for {threshold} clicks (TeslaX style click threshold)...");
            
            // Inject script to count clicks (TeslaX style)
            await _page.EvaluateAsync(@"
                (function() {
                    if (window.__lexusClickCounter) return; // Already initialized
                    
                    window.__lexusClickCounter = 0;
                    window.__lexusClickThreshold = " + threshold + @";
                    window.__lexusClickThresholdReached = false;
                    
                    const handleClick = () => {
                        window.__lexusClickCounter++;
                        console.log('[Lexus] Click count: ' + window.__lexusClickCounter);
                        
                        if (window.__lexusClickCounter >= window.__lexusClickThreshold) {
                            window.__lexusClickThresholdReached = true;
                            document.removeEventListener('click', handleClick);
                            console.log('[Lexus] Click threshold reached!');
                            // Dispatch custom event
                            window.dispatchEvent(new CustomEvent('lexusClickThresholdReached'));
                        }
                    };
                    
                    document.addEventListener('click', handleClick);
                })();
            ");

            // Wait for threshold to be reached
            var startTime = DateTime.Now;
            while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(timeoutMs))
            {
                var thresholdReached = await _page.EvaluateAsync<bool>("window.__lexusClickThresholdReached === true");
                if (thresholdReached)
                {
                    _logger.Info("Click threshold reached (TeslaX style)");
                    await Task.Delay(1000); // Wait a moment for cookies to settle (like TeslaX)
                    return;
                }
                await Task.Delay(100); // Check every 100ms
            }

            _logger.Warning($"Click threshold timeout after {timeoutMs}ms, proceeding anyway...");
        }

        /// <summary>
        /// Convert CookieData to Playwright cookie format
        /// </summary>
        private List<Cookie> ToPlaywrightCookies(List<Lexus2_0.Core.Cookies.CookieData> cookies)
        {
            return cookies.Select(c =>
            {
                var cookie = new Cookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path ?? "/",
                    HttpOnly = c.HttpOnly,
                    Secure = c.Secure,
                    SameSite = c.SameSite switch
                    {
                        "Strict" => SameSiteAttribute.Strict,
                        "Lax" => SameSiteAttribute.Lax,
                        "None" => SameSiteAttribute.None,
                        _ => SameSiteAttribute.None
                    }
                };

                // Convert Unix timestamp to float (seconds since epoch) if expiration date is set
                if (c.ExpirationDate > 0)
                {
                    cookie.Expires = (float)c.ExpirationDate;
                }

                return cookie;
            }).ToList();
        }

        public void Dispose()
        {
            try
            {
                _page?.CloseAsync();
                _browser?.CloseAsync();
                _playwright?.Dispose();
                _logger.Info("Browser disposed");
            }
            catch (Exception ex)
            {
                _logger.Error("Error disposing browser", ex);
            }
        }
    }
}

