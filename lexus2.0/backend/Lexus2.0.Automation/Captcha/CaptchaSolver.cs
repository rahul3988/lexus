using System;
using System.Linq;
using System.Threading.Tasks;
using Lexus2_0.Core.Logging;
using Lexus2_0.Core.OCR;
using Microsoft.Playwright;

namespace Lexus2_0.Automation.Captcha
{
    /// <summary>
    /// Captcha solving service (integrates OCR solutions)
    /// Supports both Tesseract and EasyOCR
    /// </summary>
    public class CaptchaSolver
    {
        private readonly ILogger _logger;
        private readonly EasyOCRService _easyOCR;
        private TesseractOCR? _tesseractOCR;
        private readonly CaptchaSolverType _solverType;

        public CaptchaSolver(ILogger logger, CaptchaSolverType solverType = CaptchaSolverType.EasyOCR)
        {
            _logger = logger;
            _solverType = solverType;
            _easyOCR = new EasyOCRService(logger);

            if (solverType == CaptchaSolverType.Tesseract)
            {
                try
                {
                    _tesseractOCR = new TesseractOCR(logger);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Tesseract initialization failed, falling back to EasyOCR: {ex.Message}");
                    _solverType = CaptchaSolverType.EasyOCR;
                }
            }
        }

        /// <summary>
        /// Solve captcha from page element
        /// </summary>
        public async Task<string?> SolveCaptchaAsync(IPage page, string captchaSelector = ".captcha-img")
        {
            try
            {
                _logger.Info("Attempting to solve captcha...");

                // Wait for captcha image
                var captchaElement = page.Locator(captchaSelector).First;
                await captchaElement.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

                // Get captcha image source
                var src = await captchaElement.GetAttributeAsync("src");
                if (string.IsNullOrEmpty(src))
                {
                    _logger.Warning("Captcha image source not found");
                    return null;
                }

                // Extract text using OCR
                string captchaText;
                if (_solverType == CaptchaSolverType.Tesseract && _tesseractOCR != null)
                {
                    captchaText = _tesseractOCR.ExtractTextFromBase64(src);
                }
                else
                {
                    captchaText = await _easyOCR.ExtractTextAsync(src);
                }

                if (string.IsNullOrWhiteSpace(captchaText))
                {
                    _logger.Warning("Failed to extract captcha text");
                    return null;
                }

                _logger.Info($"Captcha solved: {captchaText}");
                return captchaText;
            }
            catch (Exception ex)
            {
                _logger.Error("Error solving captcha", ex);
                return null;
            }
        }

        /// <summary>
        /// Solve captcha with retry logic (Tesla-style robust captcha solving)
        /// </summary>
        public async Task<bool> SolveAndSubmitCaptchaAsync(IPage page, string captchaInputSelector = "#captcha", int maxAttempts = 5)
        {
            // Try multiple captcha input selectors (Tesla-style)
            var captchaInputSelectors = new[]
            {
                captchaInputSelector,
                "#captcha",
                "input[id*='captcha']",
                "input[name*='captcha']",
                "input[placeholder*='Captcha']",
                "input[type='text']"
            };

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.Info($"Captcha solving attempt {attempt}/{maxAttempts}");

                    // Wait for captcha to be visible
                    await Task.Delay(1000);

                    var captchaText = await SolveCaptchaAsync(page);
                    if (string.IsNullOrEmpty(captchaText))
                    {
                        _logger.Warning($"Captcha text empty on attempt {attempt}, retrying...");
                        await Task.Delay(2000); // Wait before retry
                        continue;
                    }

                    // Clean captcha text (remove spaces, special chars)
                    captchaText = captchaText.Trim().Replace(" ", "").Replace("-", "").Replace("_", "");

                    if (captchaText.Length < 3)
                    {
                        _logger.Warning($"Captcha text too short: {captchaText}, retrying...");
                        await Task.Delay(2000);
                        continue;
                    }

                    // Fill captcha input - try multiple selectors
                    bool captchaFilled = false;
                    foreach (var selector in captchaInputSelectors)
                    {
                        try
                        {
                            var captchaInput = page.Locator(selector).First;
                            var count = await captchaInput.CountAsync();
                            if (count > 0)
                            {
                                var isVisible = await captchaInput.IsVisibleAsync();
                                if (isVisible)
                                {
                                    await captchaInput.ClearAsync();
                                    await Task.Delay(200);
                                    await captchaInput.FillAsync(captchaText);
                                    await Task.Delay(500);
                                    captchaFilled = true;
                                    _logger.Info($"Captcha filled using selector: {selector}");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Failed to fill captcha with selector {selector}: {ex.Message}");
                        }
                    }

                    if (!captchaFilled)
                    {
                        _logger.Warning("Could not find captcha input field, retrying...");
                        await Task.Delay(2000);
                        continue;
                    }

                    // Submit captcha - try multiple methods
                    bool captchaSubmitted = false;
                    
                    // Method 1: Press Enter
                    try
                    {
                        // Find the first visible captcha input
                        Microsoft.Playwright.ILocator? captchaInput = null;
                        foreach (var selector in captchaInputSelectors)
                        {
                            var locator = page.Locator(selector).First;
                            var count = await locator.CountAsync();
                            if (count > 0 && await locator.IsVisibleAsync())
                            {
                                captchaInput = locator;
                                break;
                            }
                        }

                        if (captchaInput != null)
                        {
                            await captchaInput.PressAsync("Enter");
                            captchaSubmitted = true;
                        }
                    }
                    catch { }

                    // Method 2: Click submit button if Enter didn't work
                    if (!captchaSubmitted)
                    {
                        var submitSelectors = new[]
                        {
                            "button[type='submit']",
                            "button:has-text('Submit')",
                            "button:has-text('SUBMIT')",
                            ".submit-btn",
                            "[class*='submit']"
                        };

                        foreach (var selector in submitSelectors)
                        {
                            try
                            {
                                var submitBtn = page.Locator(selector).First;
                                var count = await submitBtn.CountAsync();
                                if (count > 0 && await submitBtn.IsVisibleAsync())
                                {
                                    await submitBtn.ClickAsync();
                                    captchaSubmitted = true;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }

                    // Wait and check if captcha was accepted
                    await Task.Delay(3000);

                    // Check for success indicators (Tesla-style)
                    var successIndicators = new[]
                    {
                        "Payment Methods",
                        "Passenger Details",
                        "Book Ticket",
                        "Select Train"
                    };

                    var failureIndicators = new[]
                    {
                        "Enter Captcha",
                        "Invalid Captcha",
                        "Wrong Captcha",
                        "Captcha Error"
                    };

                    var bodyText = await page.TextContentAsync("body");
                    if (bodyText != null)
                    {
                        // Check for success
                        bool success = successIndicators.Any(indicator => bodyText.Contains(indicator));
                        bool failure = failureIndicators.Any(indicator => bodyText.Contains(indicator));

                        if (success && !failure)
                        {
                            _logger.Info("Captcha solved successfully!");
                            return true;
                        }

                        if (failure)
                        {
                            _logger.Warning($"Captcha rejected: {captchaText}, retrying...");
                            await Task.Delay(2000);
                            continue;
                        }

                        // If no clear indicator, check if we're on a different page (likely success)
                        var currentUrl = page.Url;
                        if (!currentUrl.Contains("login") && !currentUrl.Contains("captcha"))
                        {
                            _logger.Info("Captcha likely solved (navigated to new page)");
                            return true;
                        }
                    }

                    _logger.Warning("Captcha status unclear, retrying...");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error in captcha solving attempt {attempt}", ex);
                    await Task.Delay(2000);
                }
            }

            _logger.Error("Failed to solve captcha after maximum attempts");
            return false;
        }

        public void Dispose()
        {
            _tesseractOCR?.Dispose();
        }
    }

    public enum CaptchaSolverType
    {
        EasyOCR,
        Tesseract,
        Manual
    }
}

