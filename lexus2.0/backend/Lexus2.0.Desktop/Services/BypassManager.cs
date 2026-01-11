using System;

namespace Lexus2_0.Desktop.Services
{
    /// <summary>
    /// Manages bypass settings (captcha, retry limits, browser mode, etc.)
    /// </summary>
    public class BypassManager
    {
        private readonly BypassSettingsService _settingsService;
        private bool _captchaHandlingEnabled = true;
        private int _retryLimit = 5;
        private BrowserMode _browserMode = BrowserMode.Headless;
        private bool _autoSessionReset = false;
        private CaptchaSolverType _captchaSolverType = CaptchaSolverType.EasyOCR;

        public BypassManager(BypassSettingsService? settingsService = null)
        {
            _settingsService = settingsService ?? new BypassSettingsService();
            LoadSettings();
        }

        public bool CaptchaHandlingEnabled 
        { 
            get => _captchaHandlingEnabled;
            set => _captchaHandlingEnabled = value;
        }
        
        public int RetryLimit 
        { 
            get => _retryLimit;
            set => _retryLimit = value;
        }
        
        public BrowserMode BrowserMode 
        { 
            get => _browserMode;
            set => _browserMode = value;
        }
        
        public bool AutoSessionReset 
        { 
            get => _autoSessionReset;
            set => _autoSessionReset = value;
        }
        
        public CaptchaSolverType CaptchaSolverType 
        { 
            get => _captchaSolverType;
            set => _captchaSolverType = value;
        }

        public event EventHandler<BypassSettingsChangedEventArgs>? SettingsChanged;

        public void UpdateSettings(BypassSettings settings)
        {
            CaptchaHandlingEnabled = settings.CaptchaHandlingEnabled;
            RetryLimit = settings.RetryLimit;
            BrowserMode = settings.BrowserMode;
            AutoSessionReset = settings.AutoSessionReset;
            CaptchaSolverType = settings.CaptchaSolverType;

            SaveSettings();
            SettingsChanged?.Invoke(this, new BypassSettingsChangedEventArgs(settings));
        }

        public BypassSettings GetSettings()
        {
            return new BypassSettings
            {
                CaptchaHandlingEnabled = CaptchaHandlingEnabled,
                RetryLimit = RetryLimit,
                BrowserMode = BrowserMode,
                AutoSessionReset = AutoSessionReset,
                CaptchaSolverType = CaptchaSolverType
            };
        }

        private void LoadSettings()
        {
            var settings = _settingsService.LoadSettings();
            CaptchaHandlingEnabled = settings.CaptchaHandlingEnabled;
            RetryLimit = settings.RetryLimit;
            BrowserMode = settings.BrowserMode;
            AutoSessionReset = settings.AutoSessionReset;
            CaptchaSolverType = settings.CaptchaSolverType;
        }

        private void SaveSettings()
        {
            var settings = GetSettings();
            _settingsService.SaveSettings(settings);
        }
    }

    public class BypassSettings
    {
        public bool CaptchaHandlingEnabled { get; set; } = true;
        public int RetryLimit { get; set; } = 5;
        public BrowserMode BrowserMode { get; set; } = BrowserMode.Headless;
        public bool AutoSessionReset { get; set; } = false;
        public CaptchaSolverType CaptchaSolverType { get; set; } = CaptchaSolverType.EasyOCR;
    }

    public enum BrowserMode
    {
        Headless,
        Visible
    }

    public enum CaptchaSolverType
    {
        EasyOCR,
        Tesseract,
        Manual
    }

    public class BypassSettingsChangedEventArgs : EventArgs
    {
        public BypassSettings Settings { get; }

        public BypassSettingsChangedEventArgs(BypassSettings settings)
        {
            Settings = settings;
        }
    }
}
