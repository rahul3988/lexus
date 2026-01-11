using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lexus2_0.Core.Models;
using Lexus2_0.Core.StateMachine;
using Lexus2_0.Core.Logging;
using Lexus2_0.Core.Retry;
using Lexus2_0.Automation.Browser;
using Lexus2_0.Core.CrashRecovery;
using Lexus2_0.Automation.Captcha;
using Lexus2_0.Core.Cookies;
using Lexus2_0.Core.Proxy;

namespace Lexus2_0.Automation.Workflows
{
    /// <summary>
    /// Main booking workflow orchestrator
    /// Coordinates state machine, browser automation, and business logic
    /// </summary>
    public class BookingWorkflow
    {
        private readonly StateMachineEngine _stateMachine;
        private readonly BrowserController _browser;
        private readonly ILogger _logger;
        private readonly RetryPolicy _retryPolicy;
        private readonly CaptchaSolver _captchaSolver;
        private readonly CookieManager _cookieManager;
        private CancellationTokenSource? _cancellationTokenSource;
        private BookingConfig? _config;

        public event EventHandler<StateChangedEventArgs>? StateChanged;
        public event EventHandler<string>? LogMessage;

        public BookingState CurrentState => _stateMachine.CurrentState;

        public BookingWorkflow(ILogger logger, ProxyConfiguration? proxyConfig = null, CaptchaSolverType captchaSolverType = CaptchaSolverType.EasyOCR)
        {
            _logger = logger;
            _stateMachine = new StateMachineEngine();
            _browser = new BrowserController(logger);
            _retryPolicy = new RetryPolicy(maxRetries: 3, initialDelay: TimeSpan.FromSeconds(2));
            _captchaSolver = new CaptchaSolver(logger, captchaSolverType);
            _cookieManager = new CookieManager(logger);

            // Configure browser
            if (proxyConfig != null)
            {
                _browser.SetProxy(proxyConfig);
            }
            _browser.SetCookieManager(_cookieManager);

            _stateMachine.StateChanged += (sender, e) =>
            {
                StateChanged?.Invoke(this, e);
                LogMessage?.Invoke(this, $"State changed: {e.PreviousState} -> {e.CurrentState}");
            };
        }

        /// <summary>
        /// Start booking process
        /// </summary>
        public async Task StartAsync(BookingConfig config, CancellationToken cancellationToken = default)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _logger.Info("Starting booking workflow...");
                LogMessage?.Invoke(this, "Starting booking workflow...");

                // Initialize state machine
                _stateMachine.ExecuteAction(BookingAction.Start);
                await ProcessStateAsync(_stateMachine.CurrentState);

                // Process workflow states
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var currentState = _stateMachine.CurrentState;

                    if (currentState == BookingState.Completed || 
                        currentState == BookingState.Failed || 
                        currentState == BookingState.Stopped)
                    {
                        break;
                    }

                    if (currentState == BookingState.Paused)
                    {
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                        continue;
                    }

                    await ProcessStateAsync(currentState);
                    await Task.Delay(500, _cancellationTokenSource.Token); // Small delay between states
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Booking workflow cancelled");
                _stateMachine.ExecuteAction(BookingAction.Stop);
            }
            catch (Exception ex)
            {
                _logger.Error("Error in booking workflow", ex);
                _stateMachine.ExecuteAction(BookingAction.Error);
            }
        }

        /// <summary>
        /// Process individual state
        /// </summary>
        private async Task ProcessStateAsync(BookingState state)
        {
            _logger.Info($"Processing state: {state}");

            try
            {
                switch (state)
                {
                    case BookingState.Initializing:
                        await InitializeAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.Authenticating:
                        await AuthenticateAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.LoggingIn:
                        await LoginAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.Searching:
                        await SearchTrainAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.WaitingForTatkal:
                        await WaitForTatkalAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.SelectingTrain:
                        await SelectTrainAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.FillingDetails:
                        await FillPassengerDetailsAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;

                    case BookingState.Payment:
                        await ProcessPaymentAsync();
                        _stateMachine.ExecuteAction(BookingAction.Next);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing state {state}", ex);
                _stateMachine.ExecuteAction(BookingAction.Error);
            }
        }

        private async Task InitializeAsync()
        {
            if (_config == null) throw new InvalidOperationException("Config not set");
            
            var headless = _config.HeadlessMode;
            LogMessage?.Invoke(this, $"Initializing browser... (Headless: {headless})");
            await _browser.InitializeAsync(headless: headless);
            LogMessage?.Invoke(this, "Browser initialized");
        }

        private async Task AuthenticateAsync()
        {
            LogMessage?.Invoke(this, "Preparing authentication...");
            // Pre-authentication setup if needed
            await Task.Delay(500);
        }

        private async Task LoginAsync()
        {
            if (_config == null) throw new InvalidOperationException("Config not set");

            LogMessage?.Invoke(this, "Navigating to IRCTC...");
            await _browser.NavigateAsync("https://www.irctc.co.in/nget/train-search", timeout: 90000);
            await Task.Delay(3000); // Wait for page to fully load and stabilize

            LogMessage?.Invoke(this, "Waiting for page to be ready...");
            // Use less strict wait - just check if page loaded
            try
            {
                // Wait for DOMContentLoaded (less strict than NetworkIdle)
                await _browser.WaitForPageStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
            }
            catch
            {
                // If strict wait fails, just proceed - page might be loaded anyway
                LogMessage?.Invoke(this, "Page state check timeout, proceeding anyway...");
            }
            await Task.Delay(2000);

            // Handle Aadhaar booking window alert if it appears
            LogMessage?.Invoke(this, "Checking for alerts/dialogs...");
            await _browser.HandleAlertsAsync();
            await Task.Delay(1000);

            // Tesla-style: Check if already logged in by looking for user menu or logout button
            LogMessage?.Invoke(this, "Checking if already logged in...");
            var isLoggedIn = await CheckIfLoggedInAsync();
            if (isLoggedIn)
            {
                LogMessage?.Invoke(this, "Already logged in! Skipping login process...");
                await SaveCookiesAsync(); // Save current cookies
                return;
            }

            LogMessage?.Invoke(this, "Clicking search button to open login form...");
            // Try multiple selectors for login button (Tesla-style robust element detection)
            var loginButtonSelectors = new[]
            {
                ".h_head1 > .search_btn",
                ".search_btn",
                "button:has-text('LOGIN')",
                ".loginText",
                "[class*='login']",
                "[id*='login']"
            };

            bool loginFormOpened = false;
            foreach (var selector in loginButtonSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.ClickAsync(selector);
                        await Task.Delay(2000);
                        loginFormOpened = true;
                        LogMessage?.Invoke(this, $"Login form opened using selector: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to click login button with selector {selector}: {ex.Message}");
                }
            }

            if (!loginFormOpened)
            {
                throw new Exception("Failed to open login form - no login button found");
            }

            // Wait for login form to appear
            await Task.Delay(2000);

            LogMessage?.Invoke(this, "Filling username...");
            // Try multiple selectors for username field
            var usernameSelectors = new[]
            {
                "input[placeholder='User Name']",
                "input[placeholder*='User']",
                "input[placeholder*='Username']",
                "input[name*='user']",
                "input[id*='user']",
                "input[type='text']"
            };

            bool usernameFilled = false;
            foreach (var selector in usernameSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.FillAsync(selector, _config.Username);
                        await Task.Delay(500);
                        usernameFilled = true;
                        LogMessage?.Invoke(this, $"Username filled using selector: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to fill username with selector {selector}: {ex.Message}");
                }
            }

            if (!usernameFilled)
            {
                throw new Exception("Failed to fill username - no username field found");
            }

            LogMessage?.Invoke(this, "Filling password...");
            // Try multiple selectors for password field
            var passwordSelectors = new[]
            {
                "input[placeholder='Password']",
                "input[placeholder*='Password']",
                "input[type='password']",
                "input[name*='pass']",
                "input[id*='pass']"
            };

            bool passwordFilled = false;
            foreach (var selector in passwordSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.FillAsync(selector, _config.Password);
                        await Task.Delay(500);
                        passwordFilled = true;
                        LogMessage?.Invoke(this, $"Password filled using selector: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to fill password with selector {selector}: {ex.Message}");
                }
            }

            if (!passwordFilled)
            {
                throw new Exception("Failed to fill password - no password field found");
            }

            LogMessage?.Invoke(this, "Solving captcha...");
            await SolveCaptchaAsync();

            LogMessage?.Invoke(this, "Submitting login...");
            // Try multiple selectors for login submit button
            var submitSelectors = new[]
            {
                ".search_btn.loginText",
                "button[type='submit']",
                "button:has-text('LOGIN')",
                "button:has-text('Login')",
                ".loginText",
                "[class*='login'][class*='btn']"
            };

            bool loginSubmitted = false;
            foreach (var selector in submitSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.ClickAsync(selector);
                        loginSubmitted = true;
                        LogMessage?.Invoke(this, $"Login submitted using selector: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to click submit with selector {selector}: {ex.Message}");
                }
            }

            if (!loginSubmitted)
            {
                // Fallback: Try pressing Enter on password field
                LogMessage?.Invoke(this, "Login button not found, trying Enter key...");
                try
                {
                    var passwordField = _browser.Page!.Locator("input[type='password']").First;
                    await passwordField.PressAsync("Enter");
                    loginSubmitted = true;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to submit login with Enter key: {ex.Message}");
                }
            }

            if (!loginSubmitted)
            {
                throw new Exception("Failed to submit login - no submit button found");
            }
            
            // TeslaX style: Wait for login POST completion (detect /authprovider/webtoken)
            LogMessage?.Invoke(this, "Waiting for login POST completion (TeslaX style - webtoken detection)...");
            try
            {
                await _browser.WaitForLoginPostAsync(timeoutMs: 60000);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Login POST detection failed: {ex.Message}, proceeding anyway...");
            }
            
            await Task.Delay(3000); // Wait for page to process
            
            // Tesla-style: Verify login was successful
            LogMessage?.Invoke(this, "Verifying login success...");
            var loginSuccess = await CheckIfLoggedInAsync();
            if (!loginSuccess)
            {
                // Check for error messages
                var errorMessage = await CheckForLoginErrorAsync();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    throw new Exception($"Login failed: {errorMessage}");
                }
                throw new Exception("Login verification failed - user not logged in");
            }
            
            await _browser.WaitForPageStateAsync();
            
            // TeslaX style: Wait for click threshold (2 clicks) before saving cookies
            LogMessage?.Invoke(this, "Waiting for click threshold (TeslaX style - 2 clicks)...");
            try
            {
                await _browser.WaitForClickThresholdAsync(threshold: 2, timeoutMs: 60000);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Click threshold wait failed: {ex.Message}, proceeding anyway...");
            }
            
            // Save cookies after click threshold reached (TeslaX style)
            await SaveCookiesAsync();
            
            LogMessage?.Invoke(this, "Login completed successfully");
        }

        /// <summary>
        /// Check if user is already logged in (Tesla-style login detection)
        /// </summary>
        private async Task<bool> CheckIfLoggedInAsync()
        {
            if (_browser.Page == null) return false;

            try
            {
                // Check for common logged-in indicators
                var loggedInIndicators = new[]
                {
                    "text=Logout",
                    "text=LOGOUT",
                    "[class*='logout']",
                    "[id*='logout']",
                    "[class*='user-menu']",
                    "[id*='user-menu']",
                    ".user-name",
                    "[class*='profile']"
                };

                foreach (var indicator in loggedInIndicators)
                {
                    try
                    {
                        var element = _browser.Page.Locator(indicator).First;
                        var count = await element.CountAsync();
                        if (count > 0)
                        {
                            var isVisible = await element.IsVisibleAsync();
                            if (isVisible)
                            {
                                _logger.Info($"Login detected via indicator: {indicator}");
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        // Continue checking other indicators
                    }
                }

                // Check URL - if redirected away from login page, might be logged in
                var currentUrl = _browser.Page.Url;
                if (!currentUrl.Contains("login") && !currentUrl.Contains("auth"))
                {
                    // Additional check: try to access a protected page
                    var pageContent = await _browser.Page.TextContentAsync("body");
                    if (pageContent != null && (pageContent.Contains("Book Ticket") || pageContent.Contains("My Account")))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking login status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check for login error messages (Tesla-style error detection)
        /// </summary>
        private async Task<string> CheckForLoginErrorAsync()
        {
            if (_browser.Page == null) return string.Empty;

            try
            {
                var errorSelectors = new[]
                {
                    ".error",
                    "[class*='error']",
                    "[class*='alert']",
                    "[id*='error']",
                    ".message",
                    "[role='alert']"
                };

                foreach (var selector in errorSelectors)
                {
                    try
                    {
                        var element = _browser.Page.Locator(selector).First;
                        var count = await element.CountAsync();
                        if (count > 0)
                        {
                            var isVisible = await element.IsVisibleAsync();
                            if (isVisible)
                            {
                                var errorText = await element.TextContentAsync();
                                if (!string.IsNullOrWhiteSpace(errorText))
                                {
                                    return errorText.Trim();
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue checking other selectors
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking for login errors: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task SearchTrainAsync()
        {
            if (_config == null) throw new InvalidOperationException("Config not set");

            LogMessage?.Invoke(this, "Filling search criteria...");
            
            // Navigate to train search page if not already there
            var currentUrl = _browser.Page?.Url ?? "";
            if (!currentUrl.Contains("train-search"))
            {
                LogMessage?.Invoke(this, "Navigating to train search page...");
                await _browser.NavigateAsync("https://www.irctc.co.in/nget/train-search", timeout: 90000);
                await Task.Delay(3000);
                await _browser.WaitForPageStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
            }
            
            // Fill source station - Tesla-style with multiple selector attempts
            LogMessage?.Invoke(this, $"Entering source station: {_config.SourceStation}");
            var sourceSelectors = new[]
            {
                ".ui-autocomplete > .ng-tns-c57-8",
                "input[placeholder*='From']",
                "input[id*='source']",
                "input[name*='source']",
                ".ui-autocomplete input:first-of-type"
            };

            bool sourceFilled = false;
            foreach (var selector in sourceSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.FillAsync(selector, _config.SourceStation);
                        await Task.Delay(1500); // Wait for suggestions
                        
                        // Try to select from suggestions
                        var suggestionSelectors = new[]
                        {
                            "#p-highlighted-option",
                            ".ui-autocomplete-item:first-child",
                            "[role='option']:first-child",
                            ".ui-autocomplete-list-item:first-child"
                        };

                        foreach (var suggSelector in suggestionSelectors)
                        {
                            try
                            {
                                if (await _browser.ElementExistsAsync(suggSelector))
                                {
                                    await _browser.ClickAsync(suggSelector);
                                    sourceFilled = true;
                                    LogMessage?.Invoke(this, $"Source station selected using: {suggSelector}");
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (sourceFilled) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to fill source with selector {selector}: {ex.Message}");
                }
            }

            if (!sourceFilled)
            {
                throw new Exception("Failed to fill source station");
            }

            await Task.Delay(500);

            // Fill destination station - Tesla-style with multiple selector attempts
            LogMessage?.Invoke(this, $"Entering destination station: {_config.DestinationStation}");
            var destSelectors = new[]
            {
                ".ui-autocomplete > .ng-tns-c57-9",
                "input[placeholder*='To']",
                "input[id*='dest']",
                "input[name*='dest']",
                ".ui-autocomplete input:nth-of-type(2)"
            };

            bool destFilled = false;
            foreach (var selector in destSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.FillAsync(selector, _config.DestinationStation);
                        await Task.Delay(1500); // Wait for suggestions
                        
                        // Try to select from suggestions
                        var suggestionSelectors = new[]
                        {
                            "#p-highlighted-option",
                            ".ui-autocomplete-item:first-child",
                            "[role='option']:first-child",
                            ".ui-autocomplete-list-item:first-child"
                        };

                        foreach (var suggSelector in suggestionSelectors)
                        {
                            try
                            {
                                if (await _browser.ElementExistsAsync(suggSelector))
                                {
                                    await _browser.ClickAsync(suggSelector);
                                    destFilled = true;
                                    LogMessage?.Invoke(this, $"Destination station selected using: {suggSelector}");
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (destFilled) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to fill destination with selector {selector}: {ex.Message}");
                }
            }

            if (!destFilled)
            {
                throw new Exception("Failed to fill destination station");
            }

            await Task.Delay(500);

            // Fill date - improved calendar handling
            LogMessage?.Invoke(this, $"Selecting travel date: {_config.TravelDate}");
            await SelectTravelDateAsync(_config.TravelDate);

            // Select quota if needed - Tesla-style with better detection
            if (_config.Tatkal || _config.PremiumTatkal)
            {
                LogMessage?.Invoke(this, "Selecting quota...");
                var quotaSelectors = new[]
                {
                    "#journeyQuota > .ui-dropdown",
                    "[id*='quota']",
                    "[class*='quota']",
                    "select[id*='quota']"
                };

                bool quotaOpened = false;
                foreach (var selector in quotaSelectors)
                {
                    try
                    {
                        if (await _browser.ElementExistsAsync(selector))
                        {
                            await _browser.ClickAsync(selector);
                            await Task.Delay(1000);
                            quotaOpened = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (quotaOpened)
                {
                    if (_config.Tatkal)
                    {
                        // Try to find Tatkal option
                        var tatkalSelectors = new[]
                        {
                            ":nth-child(6) > .ui-dropdown-item",
                            ".ui-dropdown-item:has-text('Tatkal')",
                            "[role='option']:has-text('Tatkal')",
                            "li:has-text('Tatkal')"
                        };

                        foreach (var selector in tatkalSelectors)
                        {
                            try
                            {
                                if (await _browser.ElementExistsAsync(selector))
                                {
                                    await _browser.ClickAsync(selector);
                                    LogMessage?.Invoke(this, "Tatkal quota selected");
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                    else if (_config.PremiumTatkal)
                    {
                        // Try to find Premium Tatkal option
                        var premiumTatkalSelectors = new[]
                        {
                            ":nth-child(7) > .ui-dropdown-item",
                            ".ui-dropdown-item:has-text('Premium Tatkal')",
                            "[role='option']:has-text('Premium Tatkal')",
                            "li:has-text('Premium Tatkal')"
                        };

                        foreach (var selector in premiumTatkalSelectors)
                        {
                            try
                            {
                                if (await _browser.ElementExistsAsync(selector))
                                {
                                    await _browser.ClickAsync(selector);
                                    LogMessage?.Invoke(this, "Premium Tatkal quota selected");
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            // Click search - Tesla-style with multiple selector attempts
            LogMessage?.Invoke(this, "Clicking search button...");
            var searchSelectors = new[]
            {
                ".col-md-3 > .search_btn",
                ".search_btn",
                "button:has-text('Search')",
                "button:has-text('SEARCH')",
                "[class*='search'][class*='btn']",
                "[id*='search']"
            };

            bool searchClicked = false;
            foreach (var selector in searchSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.ClickAsync(selector);
                        searchClicked = true;
                        LogMessage?.Invoke(this, $"Search clicked using: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to click search with selector {selector}: {ex.Message}");
                }
            }

            if (!searchClicked)
            {
                throw new Exception("Failed to click search button");
            }

            // Wait for search results
            await Task.Delay(3000);
            await _browser.WaitForPageStateAsync();
            
            LogMessage?.Invoke(this, "Train search completed");
        }

        private async Task WaitForTatkalAsync()
        {
            if (_config == null || (!_config.Tatkal && !_config.PremiumTatkal))
            {
                return; // Skip if not tatkal booking
            }

            LogMessage?.Invoke(this, "Waiting for Tatkal booking to open...");
            
            // Poll for tatkal availability
            var maxAttempts = 300; // 5 minutes with 1 second intervals
            for (int i = 0; i < maxAttempts; i++)
            {
                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                    break;

                // Check if tatkal is available (implementation depends on IRCTC structure)
                var isAvailable = await CheckTatkalAvailabilityAsync();
                if (isAvailable)
                {
                    LogMessage?.Invoke(this, "Tatkal booking is now open!");
                    return;
                }

                await Task.Delay(1000, _cancellationTokenSource?.Token ?? CancellationToken.None);
            }

            throw new TimeoutException("Tatkal booking did not open within timeout period");
        }

        private async Task<bool> CheckTatkalAvailabilityAsync()
        {
            // Implementation to check if tatkal booking is available
            // This is a placeholder - actual implementation depends on IRCTC page structure
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Select travel date using calendar - improved method
        /// </summary>
        private async Task SelectTravelDateAsync(string travelDate)
        {
            try
            {
                // Parse date (format: DD/MM/YYYY)
                var dateParts = travelDate.Split('/');
                if (dateParts.Length != 3)
                {
                    throw new ArgumentException("Invalid date format. Expected DD/MM/YYYY");
                }

                var day = dateParts[0].TrimStart('0'); // Remove leading zero
                var month = dateParts[1].TrimStart('0');
                var year = dateParts[2];

                LogMessage?.Invoke(this, $"Parsed date - Day: {day}, Month: {month}, Year: {year}");

                // Click on calendar input field
                var calendarInput = _browser.Page!.Locator(".ui-calendar input, .ui-calendar").First;
                await calendarInput.ClickAsync();
                await Task.Delay(1000); // Wait for calendar popup

                // Try to clear existing date first
                try
                {
                    await calendarInput.ClearAsync();
                    await Task.Delay(300);
                }
                catch { }

                // Method 1: Try typing the date directly
                try
                {
                    LogMessage?.Invoke(this, "Trying to type date directly...");
                    await calendarInput.FillAsync(travelDate);
                    await Task.Delay(500);
                    
                    // Press Enter or Tab to confirm
                    await calendarInput.PressAsync("Enter");
                    await Task.Delay(500);
                    
                    // Verify date was set
                    var inputValue = await calendarInput.InputValueAsync();
                    if (inputValue.Contains(day) && inputValue.Contains(year))
                    {
                        LogMessage?.Invoke(this, "Date set successfully by typing");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, $"Direct typing failed: {ex.Message}, trying calendar picker...");
                }

                // Method 2: Use calendar picker if available
                try
                {
                    // Look for calendar popup/dropdown
                    var calendarPopup = _browser.Page!.Locator(".ui-datepicker, .p-calendar, [role='dialog']").First;
                    var popupExists = await calendarPopup.CountAsync() > 0;
                    
                    if (popupExists)
                    {
                        LogMessage?.Invoke(this, "Using calendar picker...");
                        
                        // Navigate to correct month/year
                        // Click on month/year selector if needed
                        var monthYearSelector = _browser.Page!.Locator(".ui-datepicker-month, .p-datepicker-month").First;
                        if (await monthYearSelector.CountAsync() > 0)
                        {
                            await monthYearSelector.ClickAsync();
                            await Task.Delay(500);
                        }

                        // Select the day
                        var daySelector = _browser.Page!.Locator($".ui-datepicker-calendar td a:has-text('{day}'), .p-datepicker-calendar td span:has-text('{day}')").First;
                        if (await daySelector.CountAsync() > 0)
                        {
                            await daySelector.ClickAsync();
                            await Task.Delay(500);
                            LogMessage?.Invoke(this, "Date selected using calendar picker");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, $"Calendar picker failed: {ex.Message}");
                }

                // Method 3: Fallback - just type and hope for the best
                LogMessage?.Invoke(this, "Using fallback method - typing date...");
                await calendarInput.FillAsync(travelDate);
                await calendarInput.PressAsync("Tab");
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error selecting travel date: {ex.Message}", ex);
                LogMessage?.Invoke(this, $"Date selection error: {ex.Message}");
                throw;
            }
        }

        private async Task SelectTrainAsync()
        {
            if (_config == null) throw new InvalidOperationException("Config not set");

            LogMessage?.Invoke(this, "Selecting train...");
            
            // Wait for train list to load
            await Task.Delay(2000);
            
            // Tesla-style: Try multiple strategies to find and select train
            var trainSelectors = new[]
            {
                $":nth-child(n) > .bull-back:has-text('{_config.TrainNo}'):has-text('{_config.TrainCoach}')",
                $".bull-back:has-text('{_config.TrainNo}')",
                $"[class*='train']:has-text('{_config.TrainNo}')",
                $"div:has-text('{_config.TrainNo}'):has-text('{_config.TrainCoach}')",
                $"tr:has-text('{_config.TrainNo}')"
            };

            bool trainSelected = false;
            foreach (var selector in trainSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        // Try to find the book/select button for this train
                        var trainElement = _browser.Page!.Locator(selector).First;
                        
                        // Look for book button near the train
                        var bookButtonSelectors = new[]
                        {
                            "button:has-text('Book')",
                            "button:has-text('BOOK')",
                            ".book-btn",
                            "[class*='book']",
                            "a:has-text('Book')"
                        };

                        bool bookButtonFound = false;
                        foreach (var btnSelector in bookButtonSelectors)
                        {
                            try
                            {
                                // Try to find button within the train row or nearby
                                var bookButton = trainElement.Locator(btnSelector).First;
                                var count = await bookButton.CountAsync();
                                if (count > 0)
                                {
                                    await bookButton.ClickAsync();
                                    bookButtonFound = true;
                                    trainSelected = true;
                                    LogMessage?.Invoke(this, $"Train selected using book button: {btnSelector}");
                                    break;
                                }
                            }
                            catch { }
                        }

                        // If no book button found, click on the train element itself
                        if (!bookButtonFound)
                        {
                            await trainElement.ClickAsync();
                            trainSelected = true;
                            LogMessage?.Invoke(this, $"Train selected by clicking train element: {selector}");
                        }

                        if (trainSelected) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to select train with selector {selector}: {ex.Message}");
                }
            }

            if (!trainSelected)
            {
                // Fallback: Try to find train by text content
                try
                {
                    var pageContent = await _browser.Page!.TextContentAsync("body");
                    if (pageContent != null && pageContent.Contains(_config.TrainNo))
                    {
                        // Train exists on page, try clicking anywhere on the train row
                        var allTrainRows = _browser.Page.Locator("[class*='train'], [class*='bull-back'], tr").AllAsync();
                        var rows = await allTrainRows;
                        
                        foreach (var row in rows)
                        {
                            var rowText = await row.TextContentAsync();
                            if (rowText != null && rowText.Contains(_config.TrainNo) && rowText.Contains(_config.TrainCoach))
                            {
                                await row.ClickAsync();
                                trainSelected = true;
                                LogMessage?.Invoke(this, "Train selected by text matching");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Fallback train selection failed: {ex.Message}");
                }
            }

            if (!trainSelected)
            {
                throw new Exception($"Train {_config.TrainNo} with coach {_config.TrainCoach} not found on the page");
            }

            // Wait for next page to load
            await Task.Delay(2000);
            await _browser.WaitForPageStateAsync();
            
            LogMessage?.Invoke(this, "Train selected successfully");
        }

        private async Task FillPassengerDetailsAsync()
        {
            if (_config == null) throw new InvalidOperationException("Config not set");

            LogMessage?.Invoke(this, "Filling passenger details...");

            // Wait for passenger form to load
            await Task.Delay(2000);

            // Add passengers - Tesla-style with better element detection
            for (int i = 0; i < _config.PassengerDetails.Count; i++)
            {
                if (i > 0)
                {
                    // Try multiple selectors for "Add Passenger" button
                    var addPassengerSelectors = new[]
                    {
                        ".pull-left > a > :nth-child(1)",
                        "a:has-text('Add')",
                        "button:has-text('Add Passenger')",
                        "[class*='add'][class*='passenger']"
                    };

                    bool passengerAdded = false;
                    foreach (var selector in addPassengerSelectors)
                    {
                        try
                        {
                            if (await _browser.ElementExistsAsync(selector))
                            {
                                await _browser.ClickAsync(selector);
                                await Task.Delay(1000);
                                passengerAdded = true;
                                break;
                            }
                        }
                        catch { }
                    }

                    if (!passengerAdded)
                    {
                        _logger.Warning($"Could not add passenger {i + 1}, continuing anyway...");
                    }
                }

                var passenger = _config.PassengerDetails[i];
                LogMessage?.Invoke(this, $"Filling details for passenger {i + 1}: {passenger.Name}");

                // Fill name - Tesla-style with multiple selector attempts
                var nameSelectors = new[]
                {
                    ".ui-autocomplete input",
                    "input[placeholder*='Name']",
                    "input[formcontrolname*='name']",
                    "input[id*='name']"
                };

                bool nameFilled = false;
                foreach (var selector in nameSelectors)
                {
                    try
                    {
                        var nameInputs = await _browser.Page!.Locator(selector).AllAsync();
                        if (nameInputs.Count > i)
                        {
                            await nameInputs[i].FillAsync(passenger.Name);
                            await Task.Delay(500);
                            nameFilled = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!nameFilled)
                {
                    _logger.Warning($"Could not fill name for passenger {i + 1}");
                }

                // Fill age - Tesla-style with multiple selector attempts
                var ageSelectors = new[]
                {
                    "input[formcontrolname='passengerAge']",
                    "input[placeholder*='Age']",
                    "input[formcontrolname*='age']",
                    "input[id*='age']",
                    "input[type='number']"
                };

                bool ageFilled = false;
                foreach (var selector in ageSelectors)
                {
                    try
                    {
                        var ageInputs = await _browser.Page!.Locator(selector).AllAsync();
                        if (ageInputs.Count > i)
                        {
                            await ageInputs[i].FillAsync(passenger.Age.ToString());
                            await Task.Delay(300);
                            ageFilled = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!ageFilled)
                {
                    _logger.Warning($"Could not fill age for passenger {i + 1}");
                }

                // Select gender - Tesla-style with multiple selector attempts
                var genderSelectors = new[]
                {
                    "select[formcontrolname='passengerGender']",
                    "select[id*='gender']",
                    "select[name*='gender']"
                };

                bool genderSelected = false;
                foreach (var selector in genderSelectors)
                {
                    try
                    {
                        var genderSelects = await _browser.Page!.Locator(selector).AllAsync();
                        if (genderSelects.Count > i)
                        {
                            await genderSelects[i].SelectOptionAsync(passenger.Gender);
                            await Task.Delay(300);
                            genderSelected = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!genderSelected)
                {
                    _logger.Warning($"Could not select gender for passenger {i + 1}");
                }

                // Select seat preference - Tesla-style with multiple selector attempts
                var seatSelectors = new[]
                {
                    "select[formcontrolname='passengerBerthChoice']",
                    "select[id*='berth']",
                    "select[name*='berth']",
                    "select[id*='seat']"
                };

                bool seatSelected = false;
                foreach (var selector in seatSelectors)
                {
                    try
                    {
                        var seatSelects = await _browser.Page!.Locator(selector).AllAsync();
                        if (seatSelects.Count > i)
                        {
                            await seatSelects[i].SelectOptionAsync(passenger.Seat);
                            await Task.Delay(300);
                            seatSelected = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!seatSelected)
                {
                    _logger.Warning($"Could not select seat for passenger {i + 1}");
                }

                // Select food choice - Tesla-style with multiple selector attempts
                var foodSelectors = new[]
                {
                    "select[formcontrolname='passengerFoodChoice']",
                    "select[id*='food']",
                    "select[name*='food']"
                };

                bool foodSelected = false;
                foreach (var selector in foodSelectors)
                {
                    try
                    {
                        var foodSelects = await _browser.Page!.Locator(selector).AllAsync();
                        if (foodSelects.Count > i)
                        {
                            await foodSelects[i].SelectOptionAsync(passenger.Food);
                            await Task.Delay(300);
                            foodSelected = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!foodSelected)
                {
                    _logger.Warning($"Could not select food for passenger {i + 1}");
                }

                await Task.Delay(500); // Delay between passengers
            }

            // Select boarding station if different - Tesla-style
            if (!string.IsNullOrEmpty(_config.BoardingStation))
            {
                LogMessage?.Invoke(this, $"Selecting boarding station: {_config.BoardingStation}");
                var boardingSelectors = new[]
                {
                    ".ui-dropdown.ui-widget.ui-corner-all",
                    "[id*='boarding']",
                    "[class*='boarding']",
                    "select[id*='boarding']"
                };

                bool boardingSelected = false;
                foreach (var selector in boardingSelectors)
                {
                    try
                    {
                        if (await _browser.ElementExistsAsync(selector))
                        {
                            await _browser.ClickAsync(selector);
                            await Task.Delay(1000);
                            
                            // Try to find and click the boarding station option
                            var optionSelectors = new[]
                            {
                                $"li.ui-dropdown-item:has-text('{_config.BoardingStation}')",
                                $"[role='option']:has-text('{_config.BoardingStation}')",
                                $"li:has-text('{_config.BoardingStation}')"
                            };

                            foreach (var optSelector in optionSelectors)
                            {
                                try
                                {
                                    if (await _browser.ElementExistsAsync(optSelector))
                                    {
                                        await _browser.ClickAsync(optSelector);
                                        boardingSelected = true;
                                        break;
                                    }
                                }
                                catch { }
                            }

                            if (boardingSelected) break;
                        }
                    }
                    catch { }
                }

                if (!boardingSelected)
                {
                    _logger.Warning($"Could not select boarding station: {_config.BoardingStation}");
                }
            }

            // Select payment method (UPI) - Tesla-style
            LogMessage?.Invoke(this, "Selecting payment method...");
            var paymentSelectors = new[]
            {
                "#\\32  > .ui-radiobutton > .ui-radiobutton-box",
                "[id*='payment']:has-text('UPI')",
                "input[value*='UPI']",
                "[class*='payment'][class*='upi']"
            };

            bool paymentSelected = false;
            foreach (var selector in paymentSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.ClickAsync(selector);
                        await Task.Delay(500);
                        paymentSelected = true;
                        break;
                    }
                }
                catch { }
            }

            if (!paymentSelected)
            {
                _logger.Warning("Could not select payment method, proceeding anyway...");
            }

            // Proceed - Tesla-style with multiple selector attempts
            LogMessage?.Invoke(this, "Clicking proceed button...");
            var proceedSelectors = new[]
            {
                ".train_Search",
                "button:has-text('Proceed')",
                "button:has-text('PROCEED')",
                "[class*='proceed']",
                "[id*='proceed']"
            };

            bool proceedClicked = false;
            foreach (var selector in proceedSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.ClickAsync(selector);
                        proceedClicked = true;
                        LogMessage?.Invoke(this, $"Proceed clicked using: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to click proceed with selector {selector}: {ex.Message}");
                }
            }

            if (!proceedClicked)
            {
                throw new Exception("Failed to click proceed button");
            }

            // Wait for next page
            await Task.Delay(3000);
            await _browser.WaitForPageStateAsync();

            LogMessage?.Invoke(this, "Passenger details filled successfully");
        }

        private async Task ProcessPaymentAsync()
        {
            if (_config == null) throw new InvalidOperationException("Config not set");

            LogMessage?.Invoke(this, "Processing payment...");

            // Wait for payment page to load
            await Task.Delay(2000);
            await _browser.WaitForPageStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);

            // Solve second captcha if present
            LogMessage?.Invoke(this, "Checking for payment captcha...");
            try
            {
                var captchaExists = await _browser.ElementExistsAsync(".captcha-img, [class*='captcha'], [id*='captcha']");
                if (captchaExists)
                {
                    await SolveCaptchaAsync();
                }
                else
                {
                    LogMessage?.Invoke(this, "No captcha found on payment page");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error checking/solving payment captcha: {ex.Message}");
            }

            // Select UPI payment - Tesla-style with multiple selector attempts
            LogMessage?.Invoke(this, "Selecting UPI payment method...");
            var upiSelectors = new[]
            {
                ":nth-child(3) > .col-pad",
                "[class*='upi']",
                "[id*='upi']",
                "button:has-text('UPI')",
                "[class*='payment'][class*='upi']"
            };

            bool upiSelected = false;
            foreach (var selector in upiSelectors)
            {
                try
                {
                    if (await _browser.ElementExistsAsync(selector))
                    {
                        await _browser.ClickAsync(selector);
                        await Task.Delay(1000);
                        upiSelected = true;
                        LogMessage?.Invoke(this, $"UPI selected using: {selector}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to select UPI with selector {selector}: {ex.Message}");
                }
            }

            if (!upiSelected)
            {
                _logger.Warning("Could not select UPI payment, trying alternative methods...");
            }

            // Select bank type if needed
            try
            {
                var bankTypeSelectors = new[]
                {
                    ".col-sm-9 > app-bank > #bank-type",
                    "#bank-type",
                    "[id*='bank-type']",
                    "[class*='bank'][class*='type']"
                };

                bool bankTypeSelected = false;
                foreach (var selector in bankTypeSelectors)
                {
                    try
                    {
                        if (await _browser.ElementExistsAsync(selector))
                        {
                            await _browser.ClickAsync(selector);
                            await Task.Delay(1000);
                            bankTypeSelected = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (bankTypeSelected)
                {
                    // Try to select UPI option from bank list
                    var upiOptionSelectors = new[]
                    {
                        ".col-sm-9 > app-bank > #bank-type > :nth-child(2) > table > tr > :nth-child(1) > .col-lg-12 > .border-all > .col-xs-12 > .col-pad",
                        "[class*='upi']:visible",
                        "div:has-text('UPI')",
                        "[role='option']:has-text('UPI')"
                    };

                    foreach (var selector in upiOptionSelectors)
                    {
                        try
                        {
                            if (await _browser.ElementExistsAsync(selector))
                            {
                                await _browser.ClickAsync(selector);
                                await Task.Delay(1000);
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error selecting bank type: {ex.Message}");
            }

            // Enter UPI ID if provided - Tesla-style
            if (!string.IsNullOrEmpty(_config.UpiId))
            {
                LogMessage?.Invoke(this, $"Entering UPI ID: {_config.UpiId}");
                
                var upiInputSelectors = new[]
                {
                    "#ptm-upi",
                    "[id*='upi']",
                    "input[placeholder*='UPI']",
                    "input[id*='upi-id']"
                };

                bool upiInputClicked = false;
                foreach (var selector in upiInputSelectors)
                {
                    try
                    {
                        if (await _browser.ElementExistsAsync(selector))
                        {
                            await _browser.ClickAsync(selector);
                            await Task.Delay(500);
                            upiInputClicked = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (upiInputClicked)
                {
                    // Fill UPI ID
                    var upiFillSelectors = new[]
                    {
                        ".brdr-box > :nth-child(2) > ._1WLd > :nth-child(1) > .xs-hover-box > ._Mzth > .form-ctrl",
                        "input[id*='upi']",
                        "input[type='text']:visible",
                        "input[placeholder*='UPI']"
                    };

                    bool upiFilled = false;
                    foreach (var selector in upiFillSelectors)
                    {
                        try
                        {
                            if (await _browser.ElementExistsAsync(selector))
                            {
                                await _browser.FillAsync(selector, _config.UpiId);
                                await Task.Delay(500);
                                upiFilled = true;
                                LogMessage?.Invoke(this, "UPI ID filled");
                                break;
                            }
                        }
                        catch { }
                    }

                    if (upiFilled)
                    {
                        // Click submit/pay button
                        var submitSelectors = new[]
                        {
                            ":nth-child(5) > section > .btn",
                            "button:has-text('Pay')",
                            "button:has-text('PAY')",
                            "button:has-text('Submit')",
                            "[class*='btn'][class*='pay']",
                            "[id*='submit']"
                        };

                        foreach (var selector in submitSelectors)
                        {
                            try
                            {
                                if (await _browser.ElementExistsAsync(selector))
                                {
                                    await _browser.ClickAsync(selector);
                                    LogMessage?.Invoke(this, "Payment submitted");
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            else
            {
                LogMessage?.Invoke(this, "No UPI ID provided, waiting for manual payment entry...");
            }

            // Wait for payment processing
            await Task.Delay(3000);
            
            LogMessage?.Invoke(this, "Payment initiated - waiting for user confirmation or payment gateway");
        }

        private async Task SolveCaptchaAsync()
        {
            if (_browser.Page == null)
                throw new InvalidOperationException("Browser page not initialized");

            LogMessage?.Invoke(this, "Attempting to solve captcha...");
            
            var success = await _captchaSolver.SolveAndSubmitCaptchaAsync(_browser.Page);
            
            if (!success)
            {
                throw new Exception("Failed to solve captcha after multiple attempts");
            }
            
            LogMessage?.Invoke(this, "Captcha solved successfully");
        }

        private async Task SaveCookiesAsync()
        {
            if (_browser.Page != null)
            {
                try
                {
                    var cookies = await _browser.Page.Context.CookiesAsync();
                    var cookieData = cookies.Select(c => new CookieData
                    {
                        Name = c.Name,
                        Value = c.Value,
                        Domain = c.Domain,
                        Path = c.Path,
                        ExpirationDate = c.Expires > 0 ? (double)c.Expires : 0,
                        HttpOnly = c.HttpOnly,
                        Secure = c.Secure,
                        SameSite = c.SameSite.ToString()
                    }).ToList();
                    
                    _cookieManager.SaveCookies(cookieData);
                    LogMessage?.Invoke(this, $"Saved {cookieData.Count} cookies");
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to save cookies: {ex.Message}");
                }
            }
        }

        public void Pause()
        {
            _stateMachine.ExecuteAction(BookingAction.Pause);
            LogMessage?.Invoke(this, "Workflow paused");
        }

        public void Resume()
        {
            _stateMachine.ExecuteAction(BookingAction.Resume);
            LogMessage?.Invoke(this, "Workflow resumed");
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _stateMachine.ExecuteAction(BookingAction.Stop);
            LogMessage?.Invoke(this, "Workflow stopped");
        }

        public void Dispose()
        {
            _browser?.Dispose();
            _cancellationTokenSource?.Dispose();
            _captchaSolver?.Dispose();
        }
    }
}
